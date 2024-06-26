using Azure;
using Azure.AI.OpenAI.Assistants;
using BizAssistWebApp.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace BizAssistWebApp.Controllers.Services
{
    public class WebSocketServer
    {
        private readonly ConfigurationValues _configValues;
        private readonly ILogger<WebSocketServer> _logger;
        private readonly SpeechToTextService _speechToTextService;
        private readonly TextToSpeechService _textToSpeechService;
        private readonly AssistantManager _assistantManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private AssistantsClient? _assistantsClient;
        public string? Uri { get; set; }
        public WebSocket? WebSocket { get; set; }

        public WebSocketServer(
            ConfigurationValues configValues,
            ILogger<WebSocketServer> logger,
            SpeechToTextService speechToTextService,
            TextToSpeechService textToSpeechService,
            AssistantManager assistantManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _configValues = configValues;
            _logger = logger;
            _speechToTextService = speechToTextService;
            _textToSpeechService = textToSpeechService;
            _assistantManager = assistantManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task InitWebSocketAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogError("InitWebSocketAsync Failed: HttpContext is null");
                return;
            }

            Uri ??= $"wss://{context.Request.Host}/media-streaming";

            WebSocket ??= await context.WebSockets.AcceptWebSocketAsync();

            if (_assistantsClient == null)
            {
                try
                {
                    _assistantsClient = new AssistantsClient(new Uri(_configValues.OpenAIEndpoint), new AzureKeyCredential(_configValues.OpenAIKey));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error initializing AssistantsClient: {ex.Message}");
                    _assistantsClient = null;
                    return;
                }
            }

            await ProcessWebSocketAsync(WebSocket);
        }

        private async Task ProcessWebSocketAsync(WebSocket webSocket)
        {
            byte[] buffer = new byte[2048];
            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    AudioBaseClass? audioBaseClass = JsonSerializer.Deserialize<AudioBaseClass>(json);

                    if (audioBaseClass is { Kind: "AudioData", AudioData.Data: not null })
                    {
                        byte[] audioData = Convert.FromBase64String(audioBaseClass.AudioData.Data);
                        using MemoryStream audioStream = new MemoryStream(audioData);
                        await ProcessAudioStreamAsync(audioStream);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseWebSocketAsync();
                }
            }
        }

        private async Task ProcessAudioStreamAsync(Stream audioStream)
        {
            try
            {
                if (_assistantsClient != null)
                {
                    string callerText = await _speechToTextService.ConvertSpeechToTextAsync(audioStream);

                    AssistantThread thread = await _assistantsClient.CreateThreadAsync();
                    await _assistantsClient.CreateMessageAsync(thread.Id, MessageRole.User, callerText);

                    await StreamAssistantResponseAsync(thread.Id);
                }
                else
                {
                    _logger.LogError("AssistantsClient is not valid. Cannot process the audio stream.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error processing the audio stream: {ex.Message}");
            }
            finally
            {
                await CloseWebSocketAsync();
            }
        }

        private async Task CloseWebSocketAsync()
        {
            if (WebSocket is { State: WebSocketState.Open })
            {
                try
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    _logger.LogInformation("WebSocket connection closed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error closing WebSocket connection: {ex.Message}");
                }
            }
        }

        private async Task StreamAssistantResponseAsync(string threadId)
        {
            ConcurrentQueue<string> responseQueue = new ConcurrentQueue<string>();
            CancellationTokenSource responseCts = new CancellationTokenSource();

            Task streamingTask = Task.Run(async () =>
            {
                while (!responseCts.Token.IsCancellationRequested)
                {
                    if (_textToSpeechService != null && responseQueue.TryDequeue(out string? responseChunk))
                    {
                        if (!string.IsNullOrEmpty(responseChunk))
                        {
                            await _textToSpeechService.SpeakTextAsync(responseChunk);
                        }
                    }
                    await Task.Delay(100, responseCts.Token);
                }
            }, responseCts.Token);

            if (_assistantsClient == null)
            {
                _logger.LogError("Error answering the call: _assistantsClient is null.");
                return;
            }

            CreateRunOptions runOptions = new CreateRunOptions(_assistantManager.GetFirstOrDefaultAssistantId());
            Response<ThreadRun>? run = await _assistantsClient.CreateRunAsync(threadId, runOptions, responseCts.Token);

            while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress)
            {
                await Task.Delay(500, responseCts.Token);
                run = await _assistantsClient.GetRunAsync(threadId, run.Value.Id, responseCts.Token);

                Response<PageableList<ThreadMessage>>? messages = await _assistantsClient.GetMessagesAsync(threadId, cancellationToken: responseCts.Token);
                foreach (ThreadMessage? message in messages.Value)
                {
                    if (message.Role == MessageRole.Assistant)
                    {
                        foreach (MessageTextContent content in message.ContentItems.OfType<MessageTextContent>())
                        {
                            responseQueue.Enqueue(content.Text);
                        }
                    }
                }
            }

            await responseCts.CancelAsync();
            await streamingTask;
        }

        public async Task StopAsync()
        {
            if (WebSocket == null)
            {
                _logger.LogError("Error initializing WebSocket is null!");
                return;
            }

            if (WebSocket.State == WebSocketState.Open)
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                _logger.LogInformation("WebSocket connection closed.");
            }
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (WebSocket is { State: WebSocketState.Open })
            {
                _logger.LogWarning("WebSocket connection is already open.");
                return;
            }

            if (!string.IsNullOrEmpty(Uri))
            {
                try
                {
                    ClientWebSocket webSocket = new ClientWebSocket();
                    await webSocket.ConnectAsync(new Uri(Uri), cancellationToken);
                    WebSocket = webSocket;
                    _logger.LogInformation($"WebSocket connection established with {Uri}.");

                    await ProcessWebSocketAsync(WebSocket);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to open WebSocket connection to {Uri}.");
                    await StopAsync();
                }
            }
            else
            {
                _logger.LogError("Failed to open websocket! Uri is null!");
                await StopAsync();
            }
        }
    }
}

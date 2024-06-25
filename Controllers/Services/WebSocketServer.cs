using Azure;
using Azure.AI.OpenAI.Assistants;
using BizAssistWebApp.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace BizAssistWebApp.Controllers.Services
{
    public class WebSocketServer(
        ConfigurationValues configValues,
        ILogger<WebSocketServer> logger,
        SpeechToTextService speechToTextService,
        TextToSpeechService textToSpeechService,
        AssistantManager assistantManager)
    {
        private AssistantsClient? _assistantsClient;
        public WebSocket? WebSocket { get; set; }

        public async Task ProcessWebSocketAsync()
        {
            if (WebSocket == null)
            {
                logger.LogError($"Error initializing WebSocket is null! ");

                return;
            }

            if (_assistantsClient == null)
            {

                try
                {
                    _assistantsClient = new AssistantsClient(new Uri(configValues.OpenAIEndpoint), new AzureKeyCredential(configValues.OpenAIKey));
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error initializing AssistantsClient: {ex.Message}");
                    _assistantsClient = null;
                    return;
                }

            }

            

            byte[] buffer = new byte[2048];
            while (WebSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
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
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }

        private async Task ProcessAudioStreamAsync(Stream audioStream)
        {
            try
            {
                if (_assistantsClient != null)
                {
                    string callerText = await speechToTextService.ConvertSpeechToTextAsync(audioStream);

                    AssistantThread thread = await _assistantsClient.CreateThreadAsync();
                    await _assistantsClient.CreateMessageAsync(thread.Id, MessageRole.User, callerText);

                    await StreamAssistantResponseAsync(thread.Id);
                }
                else
                {
                    logger.LogError("AssistantsClient is not valid. Cannot process the audio stream.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error processing the audio stream: {ex.Message}");
            }
        }

        private async Task StreamAssistantResponseAsync(string threadId)
        {
            ConcurrentQueue<string> responseQueue = new ConcurrentQueue<string>();
            CancellationTokenSource responseCts = new CancellationTokenSource();

            // Task to stream response as it arrives
            Task streamingTask = Task.Run(async () =>
            {
                while (!responseCts.Token.IsCancellationRequested)
                {
                    if (textToSpeechService != null && responseQueue.TryDequeue(out string? responseChunk))
                    {
                        if (!string.IsNullOrEmpty(responseChunk))
                        {
                            await textToSpeechService.SpeakTextAsync(responseChunk);
                        }
                    }
                    await Task.Delay(100, responseCts.Token);
                }
            }, responseCts.Token);

            // Start the assistant run
            if (_assistantsClient == null)
            {
                logger.LogError("Error answering the call: _assistantClient is null.");
            }
            else
            {
                CreateRunOptions runOptions = new CreateRunOptions(assistantManager.GetFirstOrDefaultAssistantId());
                Response<ThreadRun>? run = await _assistantsClient.CreateRunAsync(threadId, runOptions, responseCts.Token);

                // Continuously get the responses
                while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress)
                {
                    await Task.Delay(500, responseCts.Token);
                    run = await _assistantsClient.GetRunAsync(threadId, run.Value.Id, responseCts.Token);

                    // Check for the assistant messages
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
            }

            await responseCts.CancelAsync();
            await streamingTask;
        }

        public async Task StopAsync()
        {
            if (WebSocket is { State: WebSocketState.Open })
            {
                await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                logger.LogInformation("WebSocket connection closed.");
            }
        }
    }
}
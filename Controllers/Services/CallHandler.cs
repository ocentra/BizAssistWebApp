using Azure;
using Azure.AI.OpenAI.Assistants;
using Azure.Communication.CallAutomation;
using BizAssistWebApp.Controllers.Events;
using System.Collections.Concurrent;

namespace BizAssistWebApp.Controllers.Services
{
    public class CallHandler(
        CallAutomationClient callAutomationClient,
        ILogger<IncomingCallController> logger,
        IConfiguration configuration,
        SpeechToTextService speechToTextService,
        TextToSpeechService textToSpeechService,
        AssistantManager assistantManager,
        ConfigurationValues configValues)
    {
  
        private readonly AssistantsClient? _assistantsClient = new(new Uri(configValues.OpenAIEndpoint), new AzureKeyCredential(configValues.OpenAIKey));

        public async Task AnswerCallAsync(CallData callData)
        {
            try
            {
                string? callbackUriString = configuration["CallbackUri"];
                if (string.IsNullOrEmpty(callbackUriString))
                {
                    logger?.LogInformation("Callback URI is not configured.");
                    return;
                }

                Uri callbackUri = new Uri(callbackUriString);
                Response<AnswerCallResult> answerCallResult = await callAutomationClient.AnswerCallAsync(callData.IncomingCallContext, callbackUri);
                string callConnectionId = answerCallResult.Value.CallConnection.CallConnectionId;

                logger.LogInformation($"Call answered. Call Connection ID: {callConnectionId} callbackUri {callbackUri}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error answering the call: {ex.Message}");
            }
        }

        public async Task ProcessAudioStreamAsync(CallbackEvent callbackEvent, Stream audioStream)
        {
            try
            {
                // Convert speech to text
                if (_assistantsClient != null)
                {
                    string callerText = await speechToTextService.ConvertSpeechToTextAsync(audioStream);

                    // Create a new thread in AI Studio
                    AssistantThread thread = await _assistantsClient.CreateThreadAsync();

                    // Add the user's question to the thread
                    await _assistantsClient.CreateMessageAsync(thread.Id, MessageRole.User, callerText);

                    // Stream the assistant's response
                    await StreamAssistantResponseAsync(thread.Id);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error processing the audio stream: {ex.Message}");
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
                logger?.LogError($"Error answering the call: _assistantManager or _assistantsClient is null ");
            }
            else
            {
                CreateRunOptions runOptions = new CreateRunOptions(assistantManager.GetFirstOrDefaultAssistantId());
                Response<ThreadRun>? run =
                    await _assistantsClient.CreateRunAsync(threadId, runOptions, responseCts.Token);

                // Continuously get the responses
                while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress)
                {
                    await Task.Delay(500, responseCts.Token);
                    run = await _assistantsClient.GetRunAsync(threadId, run.Value.Id, responseCts.Token);

                    // Check for the assistant messages
                    Response<PageableList<ThreadMessage>>? messages =
                        await _assistantsClient.GetMessagesAsync(threadId, cancellationToken: responseCts.Token);
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
    }
}

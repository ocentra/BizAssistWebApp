using Azure;
using Azure.Communication.CallAutomation;
using BizAssistWebApp.Models;

namespace BizAssistWebApp.Controllers.Services
{
    public class CallHandler(
        CallAutomationClient callAutomationClient,
        ILogger<IncomingCallController> logger,
        ConfigurationValues configValues)
    {

        public async Task AnswerCallAsync(CallData callData, HttpContext context)
        {
            try
            {
                Uri callbackUri = new Uri(configValues.CallbackUri);
                string webSocketEndpoint = $"wss://{context.Request.Host}/media-streaming";

                MediaStreamingOptions mediaStreamingOptions = new MediaStreamingOptions(
                    new Uri(webSocketEndpoint),
                    MediaStreamingTransport.Websocket,
                    MediaStreamingContent.Audio,
                    MediaStreamingAudioChannel.Mixed
                );

                AnswerCallOptions answerCallOptions = new AnswerCallOptions(callData.IncomingCallContext, callbackUri)
                {
                    MediaStreamingOptions = mediaStreamingOptions
                };

                Response<AnswerCallResult> answerCallResult = await callAutomationClient.AnswerCallAsync(answerCallOptions);

                string callConnectionId = answerCallResult.Value.CallConnection.CallConnectionId;
                logger.LogInformation($"Call answered. Call Connection ID: {callConnectionId}, callbackUri: {callbackUri}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Error answering the call: {ex.Message}");
            }
        }
    }
}

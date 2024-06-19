using Azure;
using Azure.Communication.CallAutomation;
using BizAssistWebApp.Controllers.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BizAssistWebApp.Controllers.Services
{
    public class CallHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly ILogger<CallHandler> _logger;
        private readonly IConfiguration _configuration;

        public CallHandler(CallAutomationClient callAutomationClient, ILogger<CallHandler> logger, IConfiguration configuration)
        {
            _callAutomationClient = callAutomationClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task AnswerCallAsync(CallData callData)
        {
            try
            {
                // Get the callback URI from configuration
                string? callbackUriString = _configuration["CallbackUri"];
                if (string.IsNullOrEmpty(callbackUriString))
                {
                    _logger.LogInformation("Callback URI is not configured.");
                    return;
                }

                Uri callbackUri = new Uri(callbackUriString);

                // Answer the incoming call
               // Response<AnswerCallResult> answerCallResult = await _callAutomationClient.AnswerCallAsync(callData.IncomingCallContext, callbackUri);
               // string callConnectionId = answerCallResult.Value.CallConnection.CallConnectionId;

               // _logger.LogInformation($"Call answered. Call Connection ID: {callConnectionId}");

                _logger.LogInformation($"Call answered.test Call callbackUri {callbackUri}");


                // Handle other call-related actions here
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error answering the call: {ex.Message}");
            }
        }


    }
}

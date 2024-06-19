using Azure;
using Azure.Communication.PhoneNumbers;
using System.Text.Json;
using System.Text.Json.Serialization;
using BizAssistWebApp.Controllers.Services;

namespace BizAssistWebApp.Controllers.Events
{
    public class EventGridIncomingCallEvent : EventGridEvent
    {
        private readonly PhoneNumbersClient? _phoneNumbersClient;
        private readonly CallHandler _callHandler;

        public EventGridIncomingCallEvent(CallHandler callHandler)
        {
            _callHandler = callHandler;
            string? connectionString = Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_SERVICES_CONNECTION_STRING");
            if (connectionString != null)
            {
                _phoneNumbersClient = new PhoneNumbersClient(connectionString);
            }
        }

        [JsonPropertyName("data")]
        public CallData? Data { get; set; }

        public override async Task<string?> ExecuteAsync(ILogger<IncomingCallController> logger)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string formattedData = JsonSerializer.Serialize(Data, options);
            logger.LogInformation($"{Environment.NewLine} CallData: {formattedData} {Environment.NewLine}");

            if (Data?.From?.PhoneNumber?.Value != null)
            {
                string callerPhoneNumber = Data.From.PhoneNumber.Value;
                logger.LogInformation($"{Environment.NewLine} Caller Info: {callerPhoneNumber} {Environment.NewLine}");
            }

            if (Data?.To?.PhoneNumber?.Value != null)
            {
                string recipientPhoneNumber = Data.To.PhoneNumber.Value;
                logger.LogInformation($"{Environment.NewLine} Recipient Info: {recipientPhoneNumber}{Environment.NewLine}");

                if (_phoneNumbersClient != null)
                {
                    try
                    {
                        Response<PurchasedPhoneNumber>? phoneNumberResponse = await _phoneNumbersClient.GetPurchasedPhoneNumberAsync(recipientPhoneNumber);
                        logger.LogInformation($"{Environment.NewLine} Purchased Phone Number Info: {phoneNumberResponse?.Value?.PhoneNumber ?? "null"} {Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Error retrieving purchased phone number details: {ex.Message}");
                    }
                }
            }

            // Use the CallHandler to answer the call
            if (Data != null)
            {
                await _callHandler.AnswerCallAsync(Data);
            }

            return null;
        }
    }
}

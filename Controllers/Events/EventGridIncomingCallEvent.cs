using Azure;
using Azure.Communication.CallAutomation;
using Azure.Communication.PhoneNumbers;
using BizAssistWebApp.Controllers.Services;
using BizAssistWebApp.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizAssistWebApp.Controllers.Events
{
    public class EventGridIncomingCallEvent : EventGridEvent
    {
        private PhoneNumbersClient? _phoneNumbersClient;
        private CallHandler? _callHandler;
        private ILogger<IncomingCallController>? _logger;

        private HttpContext? _httpContext;

        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };


        // Parameterless constructor for deserialization
        public EventGridIncomingCallEvent() { }

        [JsonPropertyName("data")]
        public CallData? Data { get; set; }

        // Initialization method for setting up dependencies
        public void Init(ILogger<IncomingCallController> logger,
            CallAutomationClient callAutomationClient, 
            ConfigurationValues configValues,
            HttpContext httpContext)
        {
            _logger = logger;
            _httpContext = httpContext;

            _phoneNumbersClient = new PhoneNumbersClient(configValues.CommunicationServicesConnectionString);
            _callHandler = new CallHandler(
                callAutomationClient,
                logger,
                configValues);
        }



        public override async Task<string?> ExecuteAsync()
        {
            string formattedData = JsonSerializer.Serialize(Data, _serializerOptions);
            _logger?.LogInformation($"{Environment.NewLine} CallData: {formattedData} {Environment.NewLine}");

            if (Data?.From?.PhoneNumber?.Value != null)
            {
                string callerPhoneNumber = Data.From.PhoneNumber.Value;
                _logger?.LogInformation($"{Environment.NewLine} Caller Info: {callerPhoneNumber} {Environment.NewLine}");
            }

            if (Data?.To?.PhoneNumber?.Value != null)
            {
                string recipientPhoneNumber = Data.To.PhoneNumber.Value;
                _logger?.LogInformation($"{Environment.NewLine} Recipient Info: {recipientPhoneNumber}{Environment.NewLine}");

                if (_phoneNumbersClient != null)
                {
                    try
                    {
                        Response<PurchasedPhoneNumber>? phoneNumberResponse = await _phoneNumbersClient.GetPurchasedPhoneNumberAsync(recipientPhoneNumber);
                        _logger?.LogInformation($"{Environment.NewLine} Purchased Phone Number Info: {phoneNumberResponse?.Value?.PhoneNumber ?? "null"} {Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Error retrieving purchased phone number details: {ex.Message}");
                    }
                }
            }

            // Use the CallHandler to answer the call
            if (Data != null && _callHandler != null && _httpContext !=null)
            {
                await _callHandler.AnswerCallAsync(Data,_httpContext);
            }

            return null;
        }
    }
}

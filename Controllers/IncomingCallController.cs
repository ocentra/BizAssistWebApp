using BizAssistWebApp.Controllers.Events;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BizAssistWebApp.Controllers.Services;
using Azure.Communication.CallAutomation;
using Azure;
using Microsoft.IdentityModel.Tokens;

namespace BizAssistWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncomingCallController(
        ILogger<IncomingCallController> logger,
        ConfigurationValues configValues,
        CommunicationTokenService tokenService)
        : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
                logger.LogInformation($"Request Body: {requestBody}");
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                logger.LogError("Empty request body received.");
                return BadRequest("Request body is empty.");
            }

            List<EventGridEvent> eventGridEvents = ParseEventGridEvents(requestBody);

            if (!eventGridEvents.Any())
            {
                logger.LogInformation("No events found in the request body.");
                return BadRequest("No events found.");
            }

            logger.LogInformation($"Number of events received: {eventGridEvents.Count}");

            foreach (EventGridEvent evt in eventGridEvents)
            {
                switch (evt.EventType)
                {
                    case EventType.SubscriptionValidationEvent:
                        if (evt is EventGridValidationEvent validationEvent)
                        {
                            string? validationCode = await validationEvent.ExecuteAsync();
                            if (!string.IsNullOrEmpty(validationCode))
                            {
                                return Ok(new { validationResponse = validationCode });
                            }
                        }
                        break;

                    case EventType.IncomingCall:
                        if (evt is EventGridIncomingCallEvent incomingCallEvent)
                        {
                            string? communicationServicesConnectionString = await GetCommunicationServicesConnectionString();
                            if (communicationServicesConnectionString != null)
                            {

                                CallAutomationClient callAutomationClient = new CallAutomationClient(communicationServicesConnectionString);

                                incomingCallEvent.Init(
                                     logger!,
                                     callAutomationClient,
                                     configValues,
                                     HttpContext
                                 );
                                await incomingCallEvent.ExecuteAsync();
                            }
                        }
                        break;

                    default:
                        logger?.LogWarning("Unknown event type!");
                        break;
                }
            }

            return Ok("Welcome to Azure Web App!");
        }

        public async Task<string?> GetCommunicationServicesConnectionString()
        {
            if (string.IsNullOrEmpty(configValues.CommunicationServicesConnectionString))
            {
                throw new ArgumentException("Configuration values for communication services connection string cannot be null or empty.");
            }

            // Split the connection string to extract the endpoint
            string[] parts = configValues.CommunicationServicesConnectionString.Split(';');



            string? endpoint = parts.FirstOrDefault(p => p.StartsWith("endpoint=", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(endpoint))
            {
                return null;
            }

            // Generate a new token
            string token = await tokenService.GenerateTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return null;
            }
            // Construct and return the new connection string
            return $"{endpoint};accesskey={token}";
        }



        private List<EventGridEvent> ParseEventGridEvents(string json)
        {
            List<EventGridEvent> eventGridEvents = new();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in root.EnumerateArray())
                        {
                            AddEventGridEvent(element, eventGridEvents);
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        AddEventGridEvent(root, eventGridEvents);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error parsing JSON: {ex.Message}");
            }

            return eventGridEvents;
        }

        private void AddEventGridEvent(JsonElement element, List<EventGridEvent> eventGridEvents)
        {
            try
            {
                string? eventTypeString = element.GetProperty("eventType").GetString();
                EventType eventType = eventTypeString switch
                {
                    "Microsoft.EventGrid.SubscriptionValidationEvent" => EventType.SubscriptionValidationEvent,
                    "Microsoft.Communication.IncomingCall" => EventType.IncomingCall,
                    _ => EventType.Unknown
                };

                switch (eventType)
                {
                    case EventType.SubscriptionValidationEvent:
                        EventGridValidationEvent? eventGridValidationEvent = JsonSerializer.Deserialize<EventGridValidationEvent>(element.GetRawText());
                        if (eventGridValidationEvent != null)
                        {
                            eventGridEvents.Add(eventGridValidationEvent);
                        }
                        break;

                    case EventType.IncomingCall:
                        EventGridIncomingCallEvent? eventGridIncomingCallEvent = JsonSerializer.Deserialize<EventGridIncomingCallEvent>(element.GetRawText());
                        if (eventGridIncomingCallEvent != null)
                        {
                            eventGridEvents.Add(eventGridIncomingCallEvent);
                        }
                        break;

                    default:
                        EventGridEvent? eventGridEvent = JsonSerializer.Deserialize<EventGridEvent>(element.GetRawText());
                        if (eventGridEvent != null)
                        {
                            eventGridEvents.Add(eventGridEvent);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error adding event: {ex.Message}");
            }
        }
    }
}

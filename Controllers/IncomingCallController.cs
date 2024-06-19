using BizAssistWebApp.Controllers.Events;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BizAssistWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncomingCallController : ControllerBase
    {
        public static ILogger<IncomingCallController>? Logger;

        public IncomingCallController(ILogger<IncomingCallController>? logger)
        {
            Logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            string requestBody;
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
                Logger?.LogInformation($"Request Body: {requestBody}");
            }

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                Logger?.LogError("Empty request body received.");
                return BadRequest("Request body is empty.");
            }

            List<EventGridEvent> eventGridEvents = ParseEventGridEvents(requestBody, Logger);

            if (!eventGridEvents.Any())
            {
                Logger?.LogInformation("No events found in the request body.");
                return BadRequest("No events found.");
            }

            Logger?.LogInformation($"Number of events received: {eventGridEvents.Count}");

            foreach (var evt in eventGridEvents)
            {
                switch (evt.EventType)
                {
                    case EventType.SubscriptionValidationEvent:
                        if (evt is EventGridValidationEvent validationEvent)
                        {
                            var validationCode = await validationEvent.ExecuteAsync(Logger);
                            if (!string.IsNullOrEmpty(validationCode))
                            {
                                return Ok(new { validationResponse = validationCode });
                            }
                        }
                        break;

                    case EventType.IncomingCall:
                        if (evt is EventGridIncomingCallEvent incomingCallEvent)
                        {
                            await incomingCallEvent.ExecuteAsync(Logger);
                        }
                        break;

                    default:
                        Logger?.LogWarning("Unknown event type!");
                        break;
                }
            }

            return Ok("Welcome to Azure Web App!");
        }

        private List<EventGridEvent> ParseEventGridEvents(string json, ILogger<IncomingCallController>? logger)
        {
            List<EventGridEvent> eventGridEvents = new();

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement element in root.EnumerateArray())
                        {
                            AddEventGridEvent(element, eventGridEvents, logger);
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object)
                    {
                        AddEventGridEvent(root, eventGridEvents, logger);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error parsing JSON: {ex.Message}");
            }

            return eventGridEvents;
        }

        private void AddEventGridEvent(JsonElement element, List<EventGridEvent> eventGridEvents, ILogger<IncomingCallController>? logger)
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
                        eventGridEvents.Add(JsonSerializer.Deserialize<EventGridValidationEvent>(element.GetRawText()));
                        break;

                    case EventType.IncomingCall:
                        eventGridEvents.Add(JsonSerializer.Deserialize<EventGridIncomingCallEvent>(element.GetRawText()));
                        break;

                    default:
                        eventGridEvents.Add(JsonSerializer.Deserialize<EventGridEvent>(element.GetRawText()));
                        break;
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error adding event: {ex.Message}");
            }
        }
    }
}

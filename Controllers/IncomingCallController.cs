﻿using BizAssistWebApp.Controllers.Events;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BizAssistWebApp.Controllers.Services;
using Azure.Communication.CallAutomation;

namespace BizAssistWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IncomingCallController(
        ILogger<IncomingCallController> logger,
        IConfiguration configuration,
        SpeechToTextService speechToTextService,
        TextToSpeechService textToSpeechService,
        AssistantManager assistantManager,
        CallAutomationClient callAutomationClient,
        ConfigurationValues configValues)
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

            foreach (var evt in eventGridEvents)
            {
                switch (evt.EventType)
                {
                    case EventType.SubscriptionValidationEvent:
                        if (evt is EventGridValidationEvent validationEvent)
                        {
                            var validationCode = await validationEvent.ExecuteAsync();
                            if (!string.IsNullOrEmpty(validationCode))
                            {
                                return Ok(new { validationResponse = validationCode });
                            }
                        }
                        break;

                    case EventType.IncomingCall:
                        if (evt is EventGridIncomingCallEvent incomingCallEvent)
                        {
                            incomingCallEvent.Init(
                                logger!,
                                configuration,
                                speechToTextService,
                                textToSpeechService,
                                callAutomationClient,
                                assistantManager,
                                configValues
                            );
                            await incomingCallEvent.ExecuteAsync();
                        }
                        break;

                    default:
                        logger?.LogWarning("Unknown event type!");
                        break;
                }
            }

            return Ok("Welcome to Azure Web App!");
        }

        private List<EventGridEvent> ParseEventGridEvents(string json)
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
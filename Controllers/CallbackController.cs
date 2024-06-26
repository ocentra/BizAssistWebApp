using BizAssistWebApp.Controllers.Services;
using BizAssistWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BizAssistWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController(
        ILogger<CallbackController> logger,
        WebSocketServer webSocketServer) : ControllerBase

    {

        [HttpPost]
        public async Task<IActionResult> HandleCallback()
        {
            if (HttpContext.Request.IsHttps)
            {
                try
                {
                    string requestBody;
                    using (StreamReader reader = new StreamReader(Request.Body))
                    {
                        requestBody = await reader.ReadToEndAsync();
                        logger.LogInformation($"Callback received with body: {requestBody}");
                    }

                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        try
                        {
                            using (JsonDocument document = JsonDocument.Parse(requestBody))
                            {
                                // Log the type of root element
                                logger.LogInformation($"Root element type: {document.RootElement.ValueKind}");

                                if (document.RootElement.ValueKind == JsonValueKind.Object)
                                {
                                    JsonElement root = document.RootElement;

                                    // Extract and log specific properties
                                    if (root.TryGetProperty("type", out JsonElement typeElement))
                                    {
                                        string eventType = typeElement.GetString();
                                        logger.LogInformation($"Event Type: {eventType}");

                                        if (root.TryGetProperty("data", out JsonElement dataElement) && dataElement.ValueKind == JsonValueKind.Object)
                                        {
                                            if (dataElement.TryGetProperty("callConnectionId", out JsonElement callConnectionIdElement))
                                            {
                                                string callConnectionId = callConnectionIdElement.GetString();
                                                logger.LogInformation($"Call Connection ID: {callConnectionId}");
                                            }

                                            // Handle specific event types
                                            if (eventType == "Microsoft.Communication.CallConnected")
                                            {
                                                await webSocketServer.OpenAsync();
                                                logger.LogInformation("CallConnected event processed.");
                                            }
                                            else if (eventType == "Microsoft.Communication.CallDisconnected")
                                            {
                                                await webSocketServer.StopAsync();
                                                logger.LogInformation("Call disconnected event processed.");
                                            }
                                            else if (eventType == "Microsoft.Communication.ParticipantsUpdated")
                                            {
                                                if (dataElement.TryGetProperty("participants", out JsonElement participantsElement) && participantsElement.ValueKind == JsonValueKind.Array)
                                                {
                                                    var participants = JsonSerializer.Deserialize<List<Participant>>(participantsElement.GetRawText());
                                                    await HandleParticipantsUpdateAsync(participants);
                                                }
                                            }
                                            else
                                            {
                                                logger.LogWarning($"Unhandled event type: {eventType}");
                                            }
                                        }
                                        else
                                        {
                                            logger.LogError("Data element is missing or is not an object.");
                                        }
                                    }
                                    else
                                    {
                                        logger.LogError("Event type is missing.");
                                    }
                                }
                                else
                                {
                                    logger.LogError("Root element is not an object.");
                                }
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            logger.LogError(jsonEx, "JSON deserialization error: Invalid JSON structure.");
                        }
                    }

                    return Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while processing the callback.");
                    return StatusCode(500, "Internal server error.");
                }
            }
            else
            {
                logger.LogWarning("WebSocket request received at Callback endpoint, which is not supported.");
                return StatusCode(400, "WebSocket requests are not supported at this endpoint.");
            }
        }




        //[HttpPost]
        //public async Task<IActionResult> HandleCallback()
        //{
        //    if (HttpContext.Request.IsHttps)
        //    {
        //        try
        //        {
        //            string requestBody;
        //            using (StreamReader reader = new StreamReader(Request.Body))
        //            {
        //                requestBody = await reader.ReadToEndAsync();
        //                logger.LogInformation($"Callback received with body: {requestBody}");
        //            }

        //            if (!string.IsNullOrEmpty(requestBody))
        //            {
        //                try
        //                {
        //                    CallbackEvent? callbackEvent = JsonSerializer.Deserialize<CallbackEvent>(requestBody);
        //                    if (callbackEvent is { Data: not null })
        //                    {
        //                        string? dataCallConnectionId = callbackEvent.Data.CallConnectionId;

        //                        if (callbackEvent.Type == "Microsoft.Communication.CallConnected")
        //                        {
        //                            await webSocketServer.OpenAsync();

        //                            logger.LogInformation(dataCallConnectionId != null
        //                                ? $"CallConnected with dataCallConnectionId {dataCallConnectionId}"
        //                                : "CallConnected but no dataCallConnectionId");
        //                        }
        //                        else if (callbackEvent.Type == "Microsoft.Communication.CallDisconnected")
        //                        {
        //                            await webSocketServer.StopAsync();
        //                            logger.LogInformation("Call disconnected.");
        //                        }
        //                        else if (callbackEvent.Type == "Microsoft.Communication.ParticipantsUpdated")
        //                        {
        //                            List<Participant>? dataParticipants = callbackEvent.Data.Participants;
        //                            if (dataParticipants != null)
        //                            {
        //                                await HandleParticipantsUpdateAsync(dataParticipants);
        //                            }
        //                            else
        //                            {
        //                                logger.LogError("ParticipantsUpdated but no dataParticipants");
        //                            }
        //                        }
        //                        else
        //                        {
        //                            logger.LogWarning($"Unhandled event type: {callbackEvent.Type}");
        //                        }
        //                    }
        //                    else
        //                    {
        //                        logger.LogError("callbackEvent is null or data is null");
        //                    }
        //                }
        //                catch (JsonException jsonEx)
        //                {
        //                    logger.LogError(jsonEx, "JSON deserialization error: Invalid JSON structure.");
        //                }
        //            }


        //            return Ok();
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.LogError(ex, "An error occurred while processing the callback.");
        //            return StatusCode(500, "Internal server error.");
        //        }
        //    }
        //    else
        //    {
        //        logger.LogWarning("WebSocket request received at Callback endpoint, which is not supported.");
        //        return StatusCode(400, "WebSocket requests are not supported at this endpoint.");
        //    }
        //}


        public async Task HandleParticipantsUpdateAsync(List<Participant>? participants)
        {
            await Task.Delay(10);
            logger.LogInformation($"Call participants count: {participants?.Count}.");
        }
    }
}

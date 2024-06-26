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
                            CallbackEvent? callbackEvent = JsonSerializer.Deserialize<CallbackEvent>(requestBody);
                            if (callbackEvent is { Data: not null })
                            {
                                string? dataCallConnectionId = callbackEvent.Data.CallConnectionId;

                                if (callbackEvent.Type == "Microsoft.Communication.CallConnected")
                                {
                                    await webSocketServer.OpenAsync();

                                    logger.LogInformation(dataCallConnectionId != null
                                        ? $"CallConnected with dataCallConnectionId {dataCallConnectionId}"
                                        : "CallConnected but no dataCallConnectionId");
                                }
                                else if (callbackEvent.Type == "Microsoft.Communication.CallDisconnected")
                                {
                                    await webSocketServer.StopAsync();
                                    logger.LogInformation("Call disconnected.");
                                }
                                else if (callbackEvent.Type == "Microsoft.Communication.ParticipantsUpdated")
                                {
                                    List<Participant>? dataParticipants = callbackEvent.Data.Participants;
                                    if (dataParticipants != null)
                                    {
                                        await HandleParticipantsUpdateAsync(dataParticipants);
                                    }
                                    else
                                    {
                                        logger.LogError("ParticipantsUpdated but no dataParticipants");
                                    }
                                }
                                else
                                {
                                    logger.LogWarning($"Unhandled event type: {callbackEvent.Type}");
                                }
                            }
                            else
                            {
                                logger.LogError("callbackEvent is null or data is null");
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


        public async Task HandleParticipantsUpdateAsync(List<Participant> participants)
        {
            await Task.Delay(10);
            logger.LogInformation($"Call participants count: {participants.Count}.");
        }
    }
}

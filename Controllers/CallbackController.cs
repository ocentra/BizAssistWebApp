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
            try
            {
                using (StreamReader reader = new StreamReader(Request.Body))
                {
                    string requestBody = await reader.ReadToEndAsync();
                    logger.LogInformation($"Callback received with body: {requestBody}");

                    CallbackEvent? callbackEvent = JsonSerializer.Deserialize<CallbackEvent>(requestBody);

                    if (callbackEvent is { Data: not null })
                    {
                        string? dataCallConnectionId = callbackEvent.Data.CallConnectionId;

                        if (callbackEvent.Type == "Microsoft.Communication.CallConnected")
                        {
                            if (webSocketServer.WebSocket == null)
                            {
                                if (HttpContext.WebSockets.IsWebSocketRequest)
                                {

                                    webSocketServer.WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                                    await webSocketServer.ProcessWebSocketAsync();
                                }
                                else
                                {
                                    HttpContext.Response.StatusCode = 400;
                                }

                            }
         


                            logger.LogError(dataCallConnectionId != null
                                ? $"CallConnected  dataCallConnectionId{dataCallConnectionId}"
                                : $"CallConnected but no dataCallConnectionId");
                        }
                        else if (callbackEvent.Type == "Microsoft.Communication.CallDisconnected")
                        {
                            await webSocketServer.StopAsync();
                            logger.LogInformation($"Call disconnected.");

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
                                logger.LogError($"ParticipantsUpdated but no dataParticipants");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"Not defined type {callbackEvent.Type}");
                        }
                    }
                    else
                    {
                        logger.LogError($"callbackEvent is {callbackEvent == null} or data is {callbackEvent?.Data == null}");
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



        public async Task HandleParticipantsUpdateAsync(List<Participant> participants)
        {
            await Task.Delay(10);
            logger.LogInformation($"Call participants {participants.Count}.");

        }
    }
}

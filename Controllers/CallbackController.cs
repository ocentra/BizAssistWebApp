using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BizAssistWebApp.Controllers.Services;

namespace BizAssistWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly ILogger<CallbackController> _logger;
        private readonly CallHandler _callHandler;

        public CallbackController(ILogger<CallbackController> logger, CallHandler callHandler)
        {
            _logger = logger;
            _callHandler = callHandler;
        }

        [HttpPost]
        public async Task<IActionResult> HandleCallback([FromBody] CallbackEvent callbackEvent)
        {
            _logger.LogInformation($"Callback received: {System.Text.Json.JsonSerializer.Serialize(callbackEvent)}");

            // Assuming the audio data is sent in the request body
            Stream audioStream = Request.Body;

            // Pass the audio stream to CallHandler to process
            await _callHandler.ProcessAudioStreamAsync(callbackEvent, audioStream);

            return Ok();
        }
    }

    public class CallbackEvent
    {
        public string? EventType { get; set; }
        public string? CallConnectionId { get; set; }
    }
}
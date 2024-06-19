using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BizAssistWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallbackController : ControllerBase
    {
        private readonly ILogger<CallbackController> _logger;

        public CallbackController(ILogger<CallbackController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> HandleCallback([FromBody] CallbackEvent callbackEvent)
        {
            _logger.LogInformation($"Callback received: {System.Text.Json.JsonSerializer.Serialize(callbackEvent)}");

      

            return Ok();
        }

    }

    public class CallbackEvent
    {
        public string EventType { get; set; }
        public string CallConnectionId { get; set; }
    }
}

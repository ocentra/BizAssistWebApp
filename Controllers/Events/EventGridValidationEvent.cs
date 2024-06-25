using System.Text.Json.Serialization;
using BizAssistWebApp.Models;

namespace BizAssistWebApp.Controllers.Events;

public class EventGridValidationEvent : EventGridEvent
{
    [JsonPropertyName("data")]
    public ValidationData? Data { get; set; }

    public EventGridValidationEvent()
    {
            
    }
    public override Task<string?> ExecuteAsync()
    {
        string? validationCode = Data?.ValidationCode;
        return Task.FromResult(validationCode);
    }
}
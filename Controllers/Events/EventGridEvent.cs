using System.Text.Json.Serialization;

namespace BizAssistWebApp.Controllers.Events
{
    public class EventGridEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("eventType")]
        [JsonConverter(typeof(EventTypeConverter))]
        public EventType EventType { get; set; }

        [JsonPropertyName("eventTime")]
        public string? EventTime { get; set; }

        [JsonPropertyName("metadataVersion")]
        public string? MetadataVersion { get; set; }

        [JsonPropertyName("dataVersion")]
        public string? DataVersion { get; set; }

        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        public EventGridEvent()
        {
            
        }
        public virtual Task<string?> ExecuteAsync()
        {
            return Task.FromResult<string?>(null);
        }
    }
}

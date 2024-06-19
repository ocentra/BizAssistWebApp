using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizAssistWebApp.Controllers.Events
{
    public enum EventType
    {
        SubscriptionValidationEvent,
        IncomingCall,
        Unknown
    }


    public class EventTypeConverter : JsonConverter<EventType>
    {
        public override EventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? eventTypeString = reader.GetString();

            return eventTypeString switch
            {
                "Microsoft.EventGrid.SubscriptionValidationEvent" => EventType.SubscriptionValidationEvent,
                "Microsoft.Communication.IncomingCall" => EventType.IncomingCall,
                _ => EventType.Unknown
            };
        }

        public override void Write(Utf8JsonWriter writer, EventType value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

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

        public virtual Task<string?> ExecuteAsync(ILogger<IncomingCallController> logger)
        {
            logger.LogInformation($"Event ID: {Id}, Event Type: {EventType}, Data: {JsonSerializer.Serialize(this)}");
            return Task.FromResult<string?>(null);
        }
    }

    public class EventGridValidationEvent : EventGridEvent
    {
        [JsonPropertyName("data")]
        public ValidationData? Data { get; set; }

        public override Task<string?> ExecuteAsync(ILogger<IncomingCallController> logger)
        {
            string? validationCode = Data?.ValidationCode;
            logger.LogInformation($"Validation Code: {validationCode}");
            return Task.FromResult(validationCode);
        }
    }

    public class ValidationData
    {
        [JsonPropertyName("validationCode")]
        public string? ValidationCode { get; set; }

        [JsonPropertyName("validationUrl")]
        public string? ValidationUrl { get; set; }
    }

    public class CallData
    {
        [JsonPropertyName("to")]
        public CallEndpoint? To { get; set; }

        [JsonPropertyName("from")]
        public CallEndpoint? From { get; set; }

        [JsonPropertyName("serverCallId")]
        public string? ServerCallId { get; set; }

        [JsonPropertyName("callerDisplayName")]
        public string? CallerDisplayName { get; set; }

        [JsonPropertyName("incomingCallContext")]
        public string? IncomingCallContext { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }
    }

    public class CallEndpoint
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("rawId")]
        public string? RawId { get; set; }

        [JsonPropertyName("phoneNumber")]
        public PhoneNumber? PhoneNumber { get; set; }
    }

    public class PhoneNumber
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BizAssistWebApp.Controllers.Events;

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
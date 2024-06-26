using System.Text.Json.Serialization;

namespace BizAssistWebApp.Models
{
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

    public class CallbackEvent
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("data")]
        public Data? Data { get; set; }

        [JsonPropertyName("time")]
        public DateTime? Time { get; set; }

        [JsonPropertyName("specversion")]
        public string? Specversion { get; set; }

        [JsonPropertyName("datacontenttype")]
        public string? Datacontenttype { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("callConnectionId")]
        public string? CallConnectionId { get; set; }

        [JsonPropertyName("serverCallId")]
        public string? ServerCallId { get; set; }

        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        [JsonPropertyName("publicEventType")]
        public string? PublicEventType { get; set; }

        [JsonPropertyName("participants")]
        public List<Participant>? Participants { get; set; }

        [JsonPropertyName("resultInformation")]
        public ResultInformation? ResultInformation { get; set; }
    }

    public class ResultInformation
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("subCode")]
        public int SubCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }


    public class Participant
    {
        [JsonPropertyName("identifier")]
        public Identifier? Identifier { get; set; }

        [JsonPropertyName("isMuted")]
        public bool IsMuted { get; set; }
    }

    public class Identifier
    {
        [JsonPropertyName("rawId")]
        public string? RawId { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("phoneNumber")]
        public PhoneNumber? PhoneNumber { get; set; }

        [JsonPropertyName("communicationUser")]
        public CommunicationUser? CommunicationUser { get; set; }
    }

    public class CommunicationUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public class AudioBaseClass
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("audioData")]
        public AudioData? AudioData { get; set; }
    }

    public class AudioData
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }
        
        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
        
        [JsonPropertyName("participantRawID")]
        public string? ParticipantRawId { get; set; }
        
        [JsonPropertyName("silent")]
        public bool Silent { get; set; }
    }


    public class AssistantInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}

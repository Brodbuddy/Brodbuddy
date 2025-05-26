using System.Text.Json.Serialization;

namespace Core.ValueObjects;

public record OtaStatusMessage(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("progress")] int Progress,
    [property: JsonPropertyName("message")] string? Message = null
);
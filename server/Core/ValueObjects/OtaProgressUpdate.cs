using System.Text.Json.Serialization;
using Core.Interfaces;

namespace Core.ValueObjects;

public record OtaProgressUpdate(
    [property: JsonPropertyName("analyzerId")] Guid AnalyzerId,
    [property: JsonPropertyName("updateId")] Guid UpdateId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("progress")] int Progress,
    [property: JsonPropertyName("message")] string? Message
) : IBroadcastMessage;
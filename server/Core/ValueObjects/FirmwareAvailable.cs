using System.Text.Json.Serialization;
using Core.Interfaces;

namespace Core.ValueObjects;

public record FirmwareAvailable(
    [property: JsonPropertyName("firmwareId")] string FirmwareId,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("releaseNotes")] string? ReleaseNotes,
    [property: JsonPropertyName("isStable")] bool IsStable,
    [property: JsonPropertyName("fileSize")] long FileSize
) : IBroadcastMessage;
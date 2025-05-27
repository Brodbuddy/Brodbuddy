using System.Text.Json.Serialization;
using Core.Interfaces;

namespace Core.ValueObjects;

public record SourdoughReading(
    [property: JsonPropertyName("rise")] double Rise,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("humidity")] double Humidity,
    [property: JsonPropertyName("epochTime")] long EpochTime,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("localTime")] DateTime LocalTime,
    [property: JsonPropertyName("feedingNumber")] int FeedingNumber
    ) : IBroadcastMessage;
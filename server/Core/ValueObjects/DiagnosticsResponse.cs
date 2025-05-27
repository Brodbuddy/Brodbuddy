using System.Text.Json.Serialization;
using Core.Interfaces;

namespace Core.ValueObjects;

public record DiagnosticsResponse(
    [property: JsonPropertyName("analyzerId")] string AnalyzerId,
    [property: JsonPropertyName("epochTime")] long EpochTime,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("localTime")] DateTime LocalTime,
    [property: JsonPropertyName("uptime")] long Uptime,
    [property: JsonPropertyName("freeHeap")] long FreeHeap,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("wifi")] WifiInfo Wifi,
    [property: JsonPropertyName("sensors")] SensorInfo Sensors,
    [property: JsonPropertyName("humidity")] double Humidity
) : IBroadcastMessage;

public record WifiInfo(
    [property: JsonPropertyName("connected")] bool Connected,
    [property: JsonPropertyName("rssi")] int Rssi
);

public record SensorInfo(
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("humidity")] double Humidity,
    [property: JsonPropertyName("rise")] double Rise
);
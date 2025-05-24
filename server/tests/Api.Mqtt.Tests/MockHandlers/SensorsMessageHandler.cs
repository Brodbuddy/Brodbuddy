using System.Text.Json;
using Api.Mqtt.Core;
using Api.Mqtt.Tests.TestUtils;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.MockHandlers;

public class SensorsMessageHandler : IMqttMessageHandler<MockMqttPublishMessage>
{
    private readonly IMqttTestService _testService;
    
    public SensorsMessageHandler(IMqttTestService testService)
    {
        _testService = testService;
    }

    public string TopicFilter => "sensors/+/telemetry";
    public QualityOfService QoS => QualityOfService.AtMostOnceDelivery;

    public async Task HandleAsync(MockMqttPublishMessage message, OnMessageReceivedEventArgs args)
    {
        string payload = args.PublishMessage.PayloadAsString;
        var telemetry = JsonSerializer.Deserialize<DeviceTelemetry>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        
        if (telemetry != null)
        {
            await _testService.ProcessTelemetryAsync(
                telemetry.DeviceId,
                telemetry.Temperature,
                telemetry.Humidity,
                telemetry.Timestamp
            );
        }
    }
}
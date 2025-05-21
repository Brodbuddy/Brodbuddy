using Api.Mqtt.Core;
using Api.Mqtt.Tests.TestUtils;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.MockHandlers;

public interface IMqttTestService
{
    Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
}

public record DeviceTelemetry(string DeviceId, double Temperature, double Humidity, DateTime Timestamp);

public class DeviceTestMessageHandler : IMqttMessageHandler<MockMqttPublishMessage>
{
    private readonly IMqttTestService _testService;
    
    public DeviceTestMessageHandler(IMqttTestService testService)
    {
        _testService = testService;
    }

    public string TopicFilter => "devices/+/telemetry";
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(MockMqttPublishMessage message, OnMessageReceivedEventArgs args)
    {
        await _testService.ProcessTelemetryAsync("test-device", 25.0, 60.0, DateTime.UtcNow);
    }
}
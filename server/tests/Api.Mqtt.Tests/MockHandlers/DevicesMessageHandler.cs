using System.Text.Json;
using Api.Mqtt.Core;
using Api.Mqtt.MessageHandlers;
using Api.Mqtt.Tests.TestUtils;
using Application.Services;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.MockHandlers;

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
        string payload = args.PublishMessage.PayloadAsString;
        var telemetry = JsonSerializer.Deserialize<DeviceTelemetry>(
            payload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        
        if (telemetry != null)
        {
            var timestamp = DateTime.UtcNow; 

            await _testService.ProcessTelemetryAsync(
                telemetry.DeviceId,
                telemetry.Temperature,
                telemetry.Humidity,
                timestamp
            );
        }
    }
}
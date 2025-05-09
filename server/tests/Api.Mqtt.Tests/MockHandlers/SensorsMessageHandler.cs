using System.Text.Json;
using Api.Mqtt.Core;
using Api.Mqtt.MessageHandlers;
using Api.Mqtt.Tests.TestUtils;
using Application.Models;
using Application.Services;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;

namespace Api.Mqtt.Tests.MockHandlers;

public class SensorsMessageHandler : IMqttMessageHandler<MockMqttPublishMessage>
{
    private readonly ISourdoughTelemetryService _sourdoughTelemetryService;
    
    public SensorsMessageHandler(ISourdoughTelemetryService sourdoughTelemetryService)
    {
        _sourdoughTelemetryService = sourdoughTelemetryService;
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
            var timestamp = DateTime.UtcNow; 
            
            await _sourdoughTelemetryService.ProcessTelemetryAsync(new TelemetryReading(
                telemetry.DeviceId,
                telemetry.Temperature,
                telemetry.Humidity,
                timestamp
            ));
        }
    }
}
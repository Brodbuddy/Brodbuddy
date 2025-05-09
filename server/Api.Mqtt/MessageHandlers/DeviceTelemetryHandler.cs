using Api.Mqtt.Core;
using Application.Services;
using Application.Interfaces.Data.Repositories;
using Application.Models;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public record DeviceTelemetry(string DeviceId, double Temperature, double Humidity, long Timestamp);

public class DeviceTelemetryHandler : IMqttMessageHandler<DeviceTelemetry>
{
    private readonly ISourdoughTelemetryService _sourdoughTelemetryService;
    private readonly ILogger<DeviceTelemetryHandler> _logger;
   

    public DeviceTelemetryHandler(
        ISourdoughTelemetryService sourdoughTelemetryService,
        ILogger<DeviceTelemetryHandler> logger
        )
    {
        _sourdoughTelemetryService = sourdoughTelemetryService;
        _logger = logger;
    }

    public string TopicFilter => "devices/+/telemetry";
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(DeviceTelemetry message, OnMessageReceivedEventArgs args)
    {
        _logger.LogInformation("Received MQTT message: {Payload}", args.PublishMessage.PayloadAsString);
        
        var topic = args.PublishMessage.Topic;
        _logger.LogInformation("Topic: {Topic}", topic);
        
        
        var timestamp = DateTime.UtcNow;
        _logger.LogInformation(
            "Received telemetry from device {DeviceId}: Distance={Distance}mm, Rise={Rise}%", 
            message.DeviceId, 
            message.Temperature, 
            message.Humidity);
        
        var telemetryReading = new TelemetryReading(message.DeviceId, message.Temperature, message.Humidity, timestamp);
        await _sourdoughTelemetryService.ProcessTelemetryAsync(telemetryReading);
    }
}
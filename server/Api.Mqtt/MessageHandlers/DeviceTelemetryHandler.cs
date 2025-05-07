using Api.Mqtt.Core;
using Application.Services;
using Application.Interfaces.Data.Repositories; 
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public record DeviceTelemetry(string DeviceId, double Temperature, double Humidity, long Timestamp);

public class DeviceTelemetryHandler : IMqttMessageHandler<DeviceTelemetry>
{
    private readonly IMqttTestService _service;
    private readonly ILogger<DeviceTelemetryHandler> _logger;
    private readonly ITelemetryRepository _telemetryRepository; 

    public DeviceTelemetryHandler(
        IMqttTestService service,
        ILogger<DeviceTelemetryHandler> logger, 
        ITelemetryRepository telemetryRepository)
    {
        _service = service;
        _logger = logger;
        _telemetryRepository = telemetryRepository;
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

        await _telemetryRepository.SaveReadingAsync(
            message.DeviceId, 
            message.Temperature, 
            message.Humidity, 
            timestamp);
            
        await _service.ProcessTelemetryAsync(message.DeviceId, message.Temperature, message.Humidity, timestamp);
    }
}
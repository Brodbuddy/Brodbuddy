using Application.Services;
using FluentValidation;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public record DeviceTelemetry(string DeviceId, double Temperature, double Humidity, DateTime Timestamp);

public class DeviceTelemetryValidator : AbstractValidator<DeviceTelemetry>
{
    public DeviceTelemetryValidator()
    {
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.Temperature).InclusiveBetween(-50, 100);
        RuleFor(x => x.Humidity).InclusiveBetween(0, 100);
        RuleFor(x => x.Timestamp).NotEmpty();
    }
}

public class DeviceTelemetryHandler(IMqttTestService service, ILogger<DeviceTelemetryHandler> logger) : IMqttMessageHandler<DeviceTelemetry>
{
    public string TopicFilter => "devices/+/telemetry";
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(DeviceTelemetry message, string topic)
    {
        logger.LogInformation(
            "Processing telemetry from topic {Topic} - Device: {DeviceId}, Temp: {Temperature}°C, Humidity: {Humidity}%",
            topic, message.DeviceId, message.Temperature, message.Humidity);

        string deviceIdFromTopic = topic.Split('/')[1];

        if (deviceIdFromTopic != message.DeviceId)
        {
            logger.LogWarning("Device ID in message ({MessageDeviceId}) doesn't match topic ({TopicDeviceId})",
                message.DeviceId, deviceIdFromTopic);
        }
        
        await service.ProcessTelemetryAsync(message.DeviceId, message.Temperature, message.Humidity, message.Timestamp);
    }
    
    public Task HandleAsync(OnMessageReceivedEventArgs args)
    {
        // Skal lige fikse det lort her
        throw new NotImplementedException("This should be called through the dispatcher");
    }
}
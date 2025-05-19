using Api.Mqtt.Core;
using Application.Services;
using HiveMQtt.Client.Events;
using HiveMQtt.MQTT5.Types;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt.MessageHandlers;

public record DeviceTelemetry(string DeviceId, double Temperature, double Humidity, DateTime Timestamp);

public class DeviceTelemetryHandler(IMqttTestService service, ILogger<DeviceTelemetryHandler> logger) : IMqttMessageHandler<DeviceTelemetry>
{
    public string TopicFilter => "devices/+/telemetry";
    public QualityOfService QoS => QualityOfService.AtLeastOnceDelivery;

    public async Task HandleAsync(DeviceTelemetry message, OnMessageReceivedEventArgs args)
    {
        var topic = args.PublishMessage.Topic;
        logger.LogInformation("Topic: {Topic}", topic);
       
        // Vi opretter en ESP32 og tilføjer til databasen
        Guid esp32Id = Guid.Parse("a170f053-61cf-4ad7-8ece-c54aaa91869c");
        await service.ProcessTelemetryAsync(esp32Id, message.Temperature, message.Humidity, message.Timestamp);
    }
}
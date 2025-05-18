using Application.Interfaces.Communication.Publishers;
using HiveMQtt.MQTT5.Types;
using Infrastructure.Communication.Mqtt;

namespace Infrastructure.Communication.Publishers;

public class TestMqttDevicePublisher(IMqttPublisher publisher) : IDevicePublisher
{
    public async Task NotifyDeviceAsync(string deviceId, double temperature, double humidity, DateTime timestamp)
    {
        await publisher.PublishAsync("kakao", deviceId, QualityOfService.AtLeastOnceDelivery, true);
    }
}
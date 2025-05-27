using Application.Interfaces.Communication.Publishers;
using HiveMQtt.MQTT5.Types;
using Infrastructure.Communication.Mqtt;

namespace Infrastructure.Communication.Publishers;

public class AnalyzerPublisher(IMqttPublisher publisher) : IAnalyzerPublisher
{
    public async Task NotifyDeviceAsync(string deviceId, double temperature, double humidity, DateTime timestamp)
    {
        await publisher.PublishAsync("kakao", deviceId, QualityOfService.AtLeastOnceDelivery, true);
    }

    public async Task RequestDiagnosticsAsync(Guid analyzerId)
    {
        
        await publisher.PublishAsync($"analyzer/{analyzerId}/diagnostics/request", "", QualityOfService.AtLeastOnceDelivery, true);
    }
}
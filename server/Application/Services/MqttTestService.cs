using Application.Interfaces.Communication.Publishers;

namespace Application.Services;

public interface IMqttTestService
{
    Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
}

public class MqttTestService(IDevicePublisher publisher) : IMqttTestService
{
    public async Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp)
    {
        await publisher.NotifyDeviceAsync(deviceId, temperature, humidity, timestamp);
    }
}
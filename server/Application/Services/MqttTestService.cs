using Application.Interfaces;

namespace Application.Services;

public interface IMqttTestService
{
    Task ProcessTelemetryAsync(Guid esp32Id, double temperature, double humidity, DateTime timestamp);
}

public class MqttTestService(ITestNotifier notifier) : IMqttTestService
{
    public async Task ProcessTelemetryAsync(Guid esp32Id, double temperature, double humidity, DateTime timestamp)
    {
        // users_sourdoughanalyzer
        // ESP32 ID: uuid
        // USER ID: uuid
        Guid userId = Guid.Parse("38915d56-2322-4a6b-8506-a1831535e62b");
        await notifier.NotifyDeviceAsync(userId, "900 grader");
    }
}
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Communication.Publishers;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IMqttTestService
{
    Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
}
public class MqttTestService : IMqttTestService
{
    private readonly IDevicePublisher _publisher;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ILogger<MqttTestService> _logger;

    public MqttTestService(
        IDevicePublisher publisher, 
        ITelemetryRepository telemetryRepository,
        ILogger<MqttTestService> logger)
    {
        _publisher = publisher;
        _telemetryRepository = telemetryRepository;
        _logger = logger;
    }

    public async Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp)
    {
        _logger.LogInformation("Processing telemetry: DeviceId={DeviceId}, Distance={Distance}, Rise={Rise}%", 
            deviceId, temperature, humidity);
        
        await _telemetryRepository.SaveReadingAsync(deviceId, temperature, humidity, DateTime.UtcNow);
        
        await _publisher.NotifyDeviceAsync(deviceId, temperature, humidity, timestamp);
    }
}
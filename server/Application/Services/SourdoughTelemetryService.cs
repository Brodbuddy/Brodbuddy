using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Communication.Publishers;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface ISourdoughTelemetryService
{
    Task ProcessTelemetryAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
}
public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IDevicePublisher _publisher;
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly ILogger<SourdoughTelemetryService> _logger;

    public SourdoughTelemetryService(
        IDevicePublisher publisher, 
        ITelemetryRepository telemetryRepository,
        ILogger<SourdoughTelemetryService> logger)
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
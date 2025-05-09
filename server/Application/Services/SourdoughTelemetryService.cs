using Application.Interfaces.Communication.Notifier;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Communication.Publishers;
using Application.Models;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface ISourdoughTelemetryService
{
    Task ProcessTelemetryAsync(TelemetryReading telemetryReading);
}
public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly ITelemetryRepository _telemetryRepository;
    private readonly IDeviceNotifier _notifier;

    public SourdoughTelemetryService(
        ITelemetryRepository telemetryRepository,
        IDeviceNotifier notifier)
    {
        _telemetryRepository = telemetryRepository;
        _notifier = notifier;
    }

    public async Task ProcessTelemetryAsync(TelemetryReading telemetryReading)
    {
        
        await _telemetryRepository.SaveReadingAsync(telemetryReading);
        
        await _notifier.NotifyDeviceAsync(telemetryReading);
        
    }
}
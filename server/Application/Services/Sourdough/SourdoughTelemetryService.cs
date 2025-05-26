using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Data.Repositories.Sourdough;
using Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.Sourdough;

public interface ISourdoughTelemetryService
{
    Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
    Task ProcessDiagnosticsResponseAsync(Guid analyzerId, DiagnosticsResponse diagnostics);
}

public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IUserNotifier _userNotifier;
    private readonly IAdminNotifier _adminNotifier;
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
   

    public SourdoughTelemetryService(IUserNotifier userNotifier, IAdminNotifier adminNotifier, ISourdoughAnalyzerRepository analyzerRepository)
    {  
        _analyzerRepository = analyzerRepository;
        _userNotifier = userNotifier;
        _adminNotifier = adminNotifier;
    }
    
    public async Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading)
    {
        var ownerId = await _analyzerRepository.GetOwnersUserIdAsync(analyzerId);

        if (ownerId.HasValue)
        {
            await _userNotifier.NotifySourdoughReadingAsync(ownerId.Value, reading);
        }
    }
    
    public async Task ProcessDiagnosticsResponseAsync(Guid analyzerId, DiagnosticsResponse diagnostics)
    {
        var ownerId = await _analyzerRepository.GetOwnersUserIdAsync(analyzerId);

        if (ownerId.HasValue)
        {
            await _adminNotifier.NotifyDiagnosticsResponseAsync(ownerId.Value, diagnostics);
        }
    }
}
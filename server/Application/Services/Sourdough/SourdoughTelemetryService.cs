using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Communication.Publishers;
using Application.Interfaces.Data.Repositories.Sourdough;
using Core.ValueObjects;

namespace Application.Services.Sourdough;

public interface ISourdoughTelemetryService
{
    Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
    Task ProcessDiagnosticsResponseAsync(Guid analyzerId, DiagnosticsResponse diagnostics);
    Task ProcessDiagnosticsRequestAsync(Guid analyzerId);
    Task SaveSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
}

public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IUserNotifier _userNotifier;
    private readonly IAdminNotifier _adminNotifier;
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
    private readonly IAnalyzerPublisher _analyzerPublisher;
    private readonly IAnalyzerReadingRepository _readingRepository;
   

    public SourdoughTelemetryService(IUserNotifier userNotifier, IAdminNotifier adminNotifier, 
        ISourdoughAnalyzerRepository analyzerRepository, IAnalyzerPublisher analyzerPublisher,
        IAnalyzerReadingRepository readingRepository)
    {  
        _analyzerRepository = analyzerRepository;
        _userNotifier = userNotifier;
        _adminNotifier = adminNotifier;
        _analyzerPublisher = analyzerPublisher;
        _readingRepository = readingRepository;
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
    
    public async Task ProcessDiagnosticsRequestAsync(Guid analyzerId)
    {
        await _analyzerPublisher.RequestDiagnosticsAsync(analyzerId);
    }

    public async Task SaveSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading)
    {
        var userId = await _analyzerRepository.GetOwnersUserIdAsync(analyzerId);
        if (userId.HasValue)
        {
            await _readingRepository.SaveReadingAsync(reading, userId.Value, analyzerId);
        }
    }
}

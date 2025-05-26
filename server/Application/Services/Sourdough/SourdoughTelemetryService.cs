using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Data.Repositories.Sourdough;
using Core.ValueObjects;

namespace Application.Services.Sourdough;

public interface ISourdoughTelemetryService
{
    Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
    Task SaveSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
}

public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IUserNotifier _userNotifier;
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
    private readonly IAnalyzerReadingRepository _readingRepository;
   
    public SourdoughTelemetryService( IUserNotifier userNotifier, 
        ISourdoughAnalyzerRepository analyzerRepository, IAnalyzerReadingRepository readingRepository)
    {  
        _analyzerRepository = analyzerRepository;
        _userNotifier = userNotifier;
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

    public async Task SaveSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading)
    {
        var userId = await _analyzerRepository.GetOwnersUserIdAsync(analyzerId);
        if (userId.HasValue)
        {
            await _readingRepository.SaveReadingAsync(reading, userId.Value, analyzerId);
        }
    }
}
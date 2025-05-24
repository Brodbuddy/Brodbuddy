using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Data.Repositories.Sourdough;
using Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services.Sourdough;

public interface ISourdoughTelemetryService
{
    Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
}

public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IUserNotifier _userNotifier;
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
   

    public SourdoughTelemetryService(IUserNotifier userNotifier, ISourdoughAnalyzerRepository analyzerRepository)
    {  
        _analyzerRepository = analyzerRepository;
        _userNotifier = userNotifier;
    }
    
    public async Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading)
    {
        var ownerId = await _analyzerRepository.GetOwnersUserIdAsync(analyzerId);

        if (ownerId.HasValue)
        {
            await _userNotifier.NotifySourdoughReadingAsync(ownerId.Value, reading);
        }
    }
}
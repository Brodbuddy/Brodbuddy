using Application.Interfaces.Communication.Notifiers;
using Core.ValueObjects;

namespace Application.Services.Sourdough;

public interface ISourdoughTelemetryService
{
    Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading);
}

public class SourdoughTelemetryService : ISourdoughTelemetryService
{
    private readonly IUserNotifier _userNotifier;
    
    public SourdoughTelemetryService(IUserNotifier userNotifier)
    {
        _userNotifier = userNotifier;
    }
    
    public async Task ProcessSourdoughReadingAsync(Guid analyzerId, SourdoughReading reading)
    {
        await _userNotifier.NotifySourdoughReadingAsync(Guid.Parse("38915d56-2322-4a6b-8506-a1831535e62b"), reading);
    }
}
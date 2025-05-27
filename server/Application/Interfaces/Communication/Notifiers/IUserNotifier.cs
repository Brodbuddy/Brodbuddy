using Core.ValueObjects;

namespace Application.Interfaces.Communication.Notifiers;

public interface IUserNotifier
{
    Task NotifySourdoughReadingAsync(Guid userId, SourdoughReading reading);
    Task NotifyOtaProgressAsync(Guid analyzerId, OtaProgressUpdate progress);
}
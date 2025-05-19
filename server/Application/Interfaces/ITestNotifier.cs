namespace Application.Interfaces;

public interface ITestNotifier
{
    Task NotifyDeviceAsync(Guid userId, string test);
}
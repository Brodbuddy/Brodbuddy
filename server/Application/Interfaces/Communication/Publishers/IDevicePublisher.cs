namespace Application.Interfaces.Communication.Publishers;

public interface IDevicePublisher 
{
    Task NotifyDeviceAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
}
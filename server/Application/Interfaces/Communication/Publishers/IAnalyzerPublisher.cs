namespace Application.Interfaces.Communication.Publishers;

public interface IAnalyzerPublisher 
{
    Task NotifyDeviceAsync(string deviceId, double temperature, double humidity, DateTime timestamp);
    Task RequestDiagnosticsAsync(Guid analyzerId);
}
using Application.Models;

namespace Application.Interfaces.Communication.Notifier;

public interface IDeviceNotifier
{
    Task NotifyDeviceAsync(TelemetryReading telemetryReading);
}


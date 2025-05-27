using Core.ValueObjects;

namespace Application.Interfaces.Communication.Notifiers;

public interface IAdminNotifier
{
    Task NotifyDiagnosticsResponseAsync(Guid analyzerId, DiagnosticsResponse diagnostics);
}
using Application.Interfaces.Communication.Notifiers;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Core.ValueObjects;

namespace Infrastructure.Communication.Notifiers;

public class WsAdminNotifier : IAdminNotifier
{
    private readonly ISocketManager _socketManager;
    
    public WsAdminNotifier(ISocketManager socketManager)
    {
        _socketManager = socketManager;
    }

    public async Task NotifyDiagnosticsResponseAsync(Guid analyzerId, DiagnosticsResponse diagnostics)
    {
        await _socketManager.BroadcastAsync(WebSocketTopics.Admin.AllDiagnostics, diagnostics);
    }
}
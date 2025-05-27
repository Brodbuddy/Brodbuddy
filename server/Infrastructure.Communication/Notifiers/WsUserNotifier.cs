using Application.Interfaces.Communication.Notifiers;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Core.ValueObjects;

namespace Infrastructure.Communication.Notifiers;

public class WsUserNotifier : IUserNotifier
{
    private readonly ISocketManager _socketManager;
    
    public WsUserNotifier(ISocketManager socketManager)
    {
        _socketManager = socketManager;
    }

    public async Task NotifySourdoughReadingAsync(Guid userId, SourdoughReading reading)
    {
        await _socketManager.BroadcastAsync(WebSocketTopics.User.SourdoughData(userId), reading);
    }

    public async Task NotifyOtaProgressAsync(Guid analyzerId, OtaProgressUpdate progress)
    {
        await _socketManager.BroadcastAsync(WebSocketTopics.User.OtaProgress(analyzerId), progress);
    }
}
using Application.Interfaces;
using Application.Models;
using Brodbuddy.WebSocket.State;
using Core.Topics;

namespace Infrastructure.Communication.Notifiers;

sealed record SourdoughReading(string Temperature) : IBroadcastMessage { }

public class WsTestNotifier(ISocketManager manager) : ITestNotifier
{
    public async Task NotifyDeviceAsync(Guid userId, string test)
    {
        await manager.BroadcastAsync(RedisTopics.Telemetry(userId), new SourdoughReading(test));
    }
}
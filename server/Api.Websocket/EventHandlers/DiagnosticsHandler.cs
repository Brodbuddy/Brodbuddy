using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Entities;
using Core.Messaging;
using Fleck;
using Microsoft.AspNetCore.Authorization;

namespace Api.Websocket.EventHandlers;

public record SubscribeToDiagnosticsData(Guid UserId);
public record DiagnosticsDataSubscribed(Guid UserId, Guid ConnectionId);

[Authorize(Roles = Role.Admin)]
public class DiagnosticsHandler(ISocketManager manager) : ISubscriptionHandler<SubscribeToDiagnosticsData, DiagnosticsDataSubscribed>
{
    public string GetTopicKey(SubscribeToDiagnosticsData request, string clientId) => WebSocketTopics.Admin.AllDiagnostics;
    
    public async Task<DiagnosticsDataSubscribed> HandleAsync(SubscribeToDiagnosticsData incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId);
        await manager.SubscribeAsync(clientId, topic);
        return new DiagnosticsDataSubscribed(incoming.UserId, socket.ConnectionInfo.Id);
    }
}
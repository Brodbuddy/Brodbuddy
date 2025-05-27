using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Entities;
using Core.Messaging;
using Fleck;
using Microsoft.AspNetCore.Authorization;

namespace Api.Websocket.EventHandlers;

public record SubscribeToDiagnosticsData(
    [property: JsonPropertyName("userId")] Guid UserId
);

public record DiagnosticsDataSubscribed(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("connectionId")] Guid ConnectionId
);

[Authorize(Roles = Role.Admin)]
public class ResponseDiagnosticsHandler(ISocketManager manager) : ISubscriptionHandler<SubscribeToDiagnosticsData, DiagnosticsDataSubscribed>
{
    public string GetTopicKey(SubscribeToDiagnosticsData request, string clientId) => WebSocketTopics.Admin.AllDiagnostics;
    
    public async Task<DiagnosticsDataSubscribed> HandleAsync(SubscribeToDiagnosticsData incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId);
        await manager.SubscribeAsync(clientId, topic);
        return new DiagnosticsDataSubscribed(incoming.UserId, socket.ConnectionInfo.Id);
    }
}
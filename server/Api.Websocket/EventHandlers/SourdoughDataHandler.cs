using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record SubscribeToSourdoughData(
    [property: JsonPropertyName("userId")] Guid UserId
);

public record SourdoughDataSubscribed(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("connectionId")] Guid ConnectionId
);

public class SourdoughDataHandler(ISocketManager manager) : ISubscriptionHandler<SubscribeToSourdoughData, SourdoughDataSubscribed>
{
    public string GetTopicKey(SubscribeToSourdoughData request, string clientId) => WebSocketTopics.User.SourdoughData(request.UserId);
    
    public async Task<SourdoughDataSubscribed> HandleAsync(SubscribeToSourdoughData incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId);
        await manager.SubscribeAsync(clientId, topic);
        return new SourdoughDataSubscribed(incoming.UserId, socket.ConnectionInfo.Id);
    }
}
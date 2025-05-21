using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Messaging;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record SubscribeToSourdoughData(Guid UserId);
public record SourdoughDataSubscribed(Guid UserId, Guid ConnectionId);

[AllowAnonymous]
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
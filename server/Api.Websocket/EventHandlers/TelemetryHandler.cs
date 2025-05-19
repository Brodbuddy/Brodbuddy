using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Topics;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record EstablishConnection(Guid UserId);
public record ConnectionEstablished(Guid UserId, Guid ConnectionId);

// [Authorize(Roles = Role.Admin)]
[AllowAnonymous]
public class TelemetryHandler(ISocketManager manager) : ISubscriptionHandler<EstablishConnection, ConnectionEstablished>
{
    public string GetTopicKey(EstablishConnection request, string clientId) => RedisTopics.Telemetry(request.UserId); 
    
    public async Task<ConnectionEstablished> HandleAsync(EstablishConnection incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = GetTopicKey(incoming, clientId); 
        await manager.SubscribeAsync(clientId, topic);
        return new ConnectionEstablished(incoming.UserId, socket.ConnectionInfo.Id);
    }
}
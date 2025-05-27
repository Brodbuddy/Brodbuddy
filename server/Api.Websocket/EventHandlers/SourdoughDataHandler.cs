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

public class SourdoughDataHandler(ISocketManager manager) : IWebSocketHandler<SubscribeToSourdoughData, SourdoughDataSubscribed>
{
    public async Task<SourdoughDataSubscribed> HandleAsync(SubscribeToSourdoughData incoming, string clientId, IWebSocketConnection socket)
    {
        await manager.SubscribeAsync(clientId, WebSocketTopics.User.SourdoughData(incoming.UserId));
        return new SourdoughDataSubscribed(incoming.UserId, socket.ConnectionInfo.Id);
    }
}
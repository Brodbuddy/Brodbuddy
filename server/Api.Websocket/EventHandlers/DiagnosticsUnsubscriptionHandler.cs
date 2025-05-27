using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Entities;
using Core.Messaging;
using Fleck;

namespace Api.Websocket.EventHandlers;

public record UnsubscribeFromDiagnosticsData(
    [property: JsonPropertyName("userId")] Guid UserId
);

public record DiagnosticsDataUnsubscribed(
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("topic")] string Topic
);

[Authorize(Roles = Role.Admin)]
public class DiagnosticsUnsubscriptionHandler(ISocketManager manager) : IWebSocketHandler<UnsubscribeFromDiagnosticsData, DiagnosticsDataUnsubscribed>
{
    public async Task<DiagnosticsDataUnsubscribed> HandleAsync(UnsubscribeFromDiagnosticsData incoming, string clientId, IWebSocketConnection socket)
    {
        var topic = WebSocketTopics.Admin.AllDiagnostics;
        await manager.UnsubscribeAsync(clientId, topic);
        return new DiagnosticsDataUnsubscribed(incoming.UserId, topic);
    }
}
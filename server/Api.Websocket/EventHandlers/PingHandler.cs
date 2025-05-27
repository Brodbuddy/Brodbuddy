using System.Text.Json.Serialization;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Fleck;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.EventHandlers;

public record Ping(
    [property: JsonPropertyName("timestamp")] long Timestamp
);

public record Pong(
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("serverTimestamp")] long ServerTimestamp
);

[AllowAnonymous]
public class PingHandler(TimeProvider timeProvider, ILogger<PingHandler> logger) : IWebSocketHandler<Ping, Pong>
{
    public Task<Pong> HandleAsync(Ping incoming, string clientId, IWebSocketConnection socket)
    {
        logger.LogDebug("PING PONG");
        var response = new Pong(
            Timestamp: incoming.Timestamp,
            ServerTimestamp: timeProvider.GetUtcNow().ToUnixTimeMilliseconds()
        );

        return Task.FromResult(response);
    }
}
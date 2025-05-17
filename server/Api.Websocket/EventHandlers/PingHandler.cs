using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Fleck;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.EventHandlers;

public record Ping(long Timestamp);
public record Pong(long Timestamp, long ServerTimestamp);

[AllowAnonymous]
public class PingHandler(TimeProvider timeProvider, ILogger<PingHandler> logger) : IWebSocketHandler<Ping, Pong>
{
    public Task<Pong> HandleAsync(Ping incoming, string clientId, IWebSocketConnection socket)
    {
        logger.LogInformation("PING PONG");
        var response = new Pong(
            Timestamp: incoming.Timestamp,
            ServerTimestamp: timeProvider.GetUtcNow().ToUnixTimeMilliseconds()
        );

        return Task.FromResult(response);
    }
}
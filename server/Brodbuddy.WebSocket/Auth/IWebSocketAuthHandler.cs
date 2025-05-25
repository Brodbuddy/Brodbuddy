using Fleck;

namespace Brodbuddy.WebSocket.Auth;

public interface IWebSocketAuthHandler
{
    Task<WebSocketAuthResult> AuthenticateAsync(IWebSocketConnection connection, string? token, string messageType);
}
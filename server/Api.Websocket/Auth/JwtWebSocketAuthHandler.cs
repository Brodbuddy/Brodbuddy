using Application.Interfaces.Auth;
using Brodbuddy.WebSocket.Auth;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.Auth
{
    public class JwtWebSocketAuthHandler : IWebSocketAuthHandler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JwtWebSocketAuthHandler> _logger;

        public JwtWebSocketAuthHandler(
            IServiceProvider serviceProvider,
            ILogger<JwtWebSocketAuthHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<WebSocketAuthResult> AuthenticateAsync(IWebSocketConnection connection, string? token, string messageType)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No token provided for message type {MessageType}", messageType);
                return new WebSocketAuthResult(false);
            }
            
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token["Bearer ".Length..].Trim();
            }

            // Create a scope to resolve the scoped service
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            
            var authResult = await authService.ValidateTokenAsync(token);

            if (!authResult.IsAuthenticated)
            {
                _logger.LogDebug("Invalid token for message type {MessageType}", messageType);
                return new WebSocketAuthResult(false);
            }

            _logger.LogDebug("Successfully authenticated user {UserId} for message type {MessageType}",
                authResult.UserId, messageType);

            return new WebSocketAuthResult(
                true,
                authResult.UserId,
                authResult.Roles);
        }
    }
}
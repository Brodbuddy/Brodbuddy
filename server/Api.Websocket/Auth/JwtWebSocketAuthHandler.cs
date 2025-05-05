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

            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            
            var authResult = await authService.ValidateTokenAsync(token);

            return new WebSocketAuthResult(authResult.IsAuthenticated, authResult.UserId, authResult.Roles);
        }
    }
}
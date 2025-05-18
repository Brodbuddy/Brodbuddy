using System.Text.Json;
using Application.Services;
using Brodbuddy.WebSocket.Core;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.Middleware;

public class FeatureToggleWebSocketMiddleware : IWebSocketMiddleware
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeatureToggleWebSocketMiddleware> _logger;
    
    public FeatureToggleWebSocketMiddleware(
        IServiceScopeFactory scopeFactory,
        ILogger<FeatureToggleWebSocketMiddleware> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    public async Task<bool> InvokeAsync(IWebSocketConnection socket, string message, Func<Task> next)
    {
        using var scope = _scopeFactory.CreateScope();
        var toggleService = scope.ServiceProvider.GetRequiredService<IFeatureToggleService>();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        
        try 
        {
            var options = new JsonDocumentOptions 
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            
            var root = JsonDocument.Parse(message, options).RootElement;
            
            string? messageType = null;
            string? token = null;
            var requestId = "unknown";
            
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase))
                {
                    messageType = property.Value.GetString();
                }
                else if (property.Name.Equals("Token", StringComparison.OrdinalIgnoreCase))
                {
                    token = property.Value.GetString();
                }
                else if (property.Name.Equals("RequestId", StringComparison.OrdinalIgnoreCase))
                {
                    requestId = property.Value.GetString() ?? "unknown";
                }
            }
            
            if (!string.IsNullOrEmpty(messageType)) 
            {
                var featureName = $"Websocket.{messageType}";
                var isFeatureEnabled = await IsFeatureEnabledForContext(featureName, token, toggleService, jwtService);
                
                if (!isFeatureEnabled)
                {
                    await SendFeatureDisabledError(socket, messageType, requestId);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in feature toggle middleware: {Message}", ex.Message);
        }
        
        await next();
        return true;
    }
    
    private static async Task<bool> IsFeatureEnabledForContext(string featureName, string? token, IFeatureToggleService toggleService, IJwtService jwtService)
    {
        var isGloballyEnabled = await toggleService.IsEnabledAsync(featureName);
        
        if (string.IsNullOrEmpty(token))
        {
            return isGloballyEnabled;
        }
        
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = token["Bearer ".Length..].Trim();
        }
        
        if (jwtService.TryValidate(token, out var claims) && Guid.TryParse(claims.Sub, out var userId))
        {
            return await toggleService.IsEnabledForUserAsync(featureName, userId);
        }
        
        return isGloballyEnabled;
    }
    
    private async Task SendFeatureDisabledError(IWebSocketConnection socket, string messageType, string requestId)
    {
        _logger.LogInformation("WebSocket handler {Handler} is disabled", messageType);
        
        await socket.Send(JsonSerializer.Serialize(new 
        {
            Type = "Error",
            RequestId = requestId,
            Payload = new 
            {
                Code = "FEATURE_DISABLED",
                Message = "This feature is currently disabled"
            }
        }));
    }
}
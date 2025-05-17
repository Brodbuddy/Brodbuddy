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
        
        try {
            var options = new JsonDocumentOptions {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            
            var root = JsonDocument.Parse(message, options).RootElement;
            
            string? messageType = null;
            foreach (var property in root.EnumerateObject())
            {
                if (!property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase)) continue;
                messageType = property.Value.GetString();
                break;
            }
            
            if (!string.IsNullOrEmpty(messageType)) 
            {
                var featureName = $"Websocket.{messageType}";
                
                if (!toggleService.IsEnabled(featureName)) 
                {
                    _logger.LogInformation("WebSocket handler {Handler} is disabled", messageType);
                    
                    var requestId = "unknown";
                    foreach (var property in root.EnumerateObject())
                    {
                        if (!property.Name.Equals("RequestId", StringComparison.OrdinalIgnoreCase)) continue;
                        requestId = property.Value.GetString() ?? "unknown";
                        break;
                    }
                    
                    await socket.Send(JsonSerializer.Serialize(new {
                        Type = "Error",
                        RequestId = requestId,
                        Payload = new {
                            Code = "FEATURE_DISABLED",
                            Message = "This feature is currently disabled"
                        }
                    }));
                    
                    return false; // Stop middleware pipeline
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
}
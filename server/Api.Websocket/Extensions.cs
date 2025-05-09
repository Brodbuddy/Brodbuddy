using Api.Websocket.Auth;
using Api.Websocket.ExceptionHandler;
using Api.Websocket.Spec;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Websocket;

public static class Extensions
{
    public static IServiceCollection AddWebsocketApi(this IServiceCollection services)
    {
        services.AddWebSocketHandlers(typeof(FleckWebSocketServer).Assembly);
        services.AddHostedService<FleckWebSocketServer>();
        
        services.AddSingleton<IWebSocketAuthHandler, JwtWebSocketAuthHandler>();
        services.AddSingleton<IWebSocketExceptionHandler, GlobalWebsocketExceptionHandler>();
        
        services.GenerateClientApi();
        
        return services;
    }

    private static IServiceCollection GenerateClientApi(this IServiceCollection services)
    {
        var baseDir = Directory.GetCurrentDirectory();
        var templatesDir = Path.Combine(baseDir, "../Api.Websocket/Spec");
        var outputDir = Path.Combine(baseDir, "../../client/src/api");
        Directory.CreateDirectory(outputDir);
        
        var spec = SpecGenerator.GenerateSpec(typeof(FleckWebSocketServer).Assembly, services.BuildServiceProvider());
        TypeScriptGenerator.Generate(spec, templatesDir, outputDir);
        
        return services;
    }
    
    public static WebApplication ConfigureWebsocketApi(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<WebSocketDispatcher>();
        dispatcher.RegisterHandlers(typeof(FleckWebSocketServer).Assembly);
        return app;
    }
}
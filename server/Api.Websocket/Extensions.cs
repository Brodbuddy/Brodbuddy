using Api.Websocket.Auth;
using Api.Websocket.ExceptionHandler;
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

        return services;
    }

    public static WebApplication ConfigureWebsocketApi(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<WebSocketDispatcher>();
        dispatcher.RegisterHandlers(typeof(FleckWebSocketServer).Assembly);
        return app;
    }
}
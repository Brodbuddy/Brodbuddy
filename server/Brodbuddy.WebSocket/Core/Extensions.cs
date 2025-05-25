using System.Reflection;
using Brodbuddy.WebSocket.Auth;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brodbuddy.WebSocket.Core;

public static class WebSocketExtensions
{
    public static IServiceCollection AddWebSocketHandlers(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWebSocketHandler<,>)));

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }
        
        var validatorTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true } && 
                        t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>));
                                                 
        foreach (var validatorType in validatorTypes)
        {
            services.AddScoped(validatorType);
        }

        services.TryAddSingleton<IWebSocketAuthHandler>(_ => null!);
        services.AddSingleton<WebSocketDispatcher>();
        services.AddSingleton<IWebSocketExceptionHandler, WebSocketExceptionHandler>();
        
        return services;
    }
}
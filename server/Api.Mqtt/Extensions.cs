using System.Reflection;
using Application;
using FluentValidation;
using HiveMQtt.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Mqtt;

public static class Extensions
{
    public static IServiceCollection AddMqttApi(this IServiceCollection services)
    {
        services.AddSingleton<HiveMQClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AppOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<HiveMQClient>>();

            var clientOptions = new HiveMQClientOptionsBuilder()
                .WithWebSocketServer($"ws://{options.Mqtt.Host}:{options.Mqtt.WebSocketPort}/mqtt")
                .WithClientId($"backend_{Guid.NewGuid()}")
                .WithUserName(options.Mqtt.Username)
                .WithPassword(options.Mqtt.Password)
                .WithCleanStart(true)
                .WithAutomaticReconnect(true)
                .WithKeepAlive(30)
                .WithMaximumPacketSize(1024)
                .WithReceiveMaximum(100)
                .WithSessionExpiryInterval(3600)
                .WithRequestProblemInformation(true)
                .WithRequestResponseInformation(true)
                .WithAllowInvalidBrokerCertificates(true)
                .Build();

            var client = new HiveMQClient(clientOptions);
            
            client.AfterConnect += (s, e) => logger.LogInformation("MQTT Client Connected. Result: {Reason}", e.ConnectResult.ReasonString);
            client.AfterDisconnect += (s, e) => logger.LogWarning("MQTT Client Disconnected. Clean disconnect: {CleanDisconnect}", e.CleanDisconnect);

            return client;
        });
        
        services.AddSingleton<MqttDispatcher>();

        services.AddHostedService<MqttHostedService>();
        RegisterMqttHandlersAndValidators(services, typeof(Extensions).Assembly);

        return services;
    }
    
    private static void RegisterMqttHandlersAndValidators(IServiceCollection services, Assembly assembly)
    {
        // Register alle message handlers
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.GetInterfaces().Any(i => i == typeof(IMqttMessageHandler) ||
                                                   (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMqttMessageHandler<>))))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(handlerType);
        }

        // Register validators
        var validatorTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, BaseType.IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToList();

        foreach (var validatorType in validatorTypes)
        {
            var entityType = validatorType.BaseType!.GetGenericArguments()[0];
            var validatorInterface = typeof(IValidator<>).MakeGenericType(entityType);
            services.AddScoped(validatorInterface, validatorType);
        }
    }
    
    public static WebApplication ConfigureMqttApi(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<MqttDispatcher>();
        dispatcher.RegisterHandlers(typeof(MqttHostedService).Assembly);
        return app;
    }
}
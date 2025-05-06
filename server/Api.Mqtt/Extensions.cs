using System.Reflection;
using Api.Mqtt.Core;
using Api.Mqtt.Routing;
using Api.Mqtt.Service;
using Application;
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
                .WithWebSocketServer($"wss://{options.Mqtt.Host}:{options.Mqtt.WebSocketPort}/mqtt")
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
        RegisterMqttHandlersAndValidators(services, typeof(MqttHostedService).Assembly);

        return services;
    }
    
    private static void RegisterMqttHandlersAndValidators(IServiceCollection services, Assembly assembly)
    {
        foreach (var handlerType in HandlerTypeHelpers.GetMqttMessageHandlers(assembly))
        {
            services.AddScoped(handlerType);
        }
    }
    
    public static WebApplication ConfigureMqttApi(this WebApplication app)
    {
        var dispatcher = app.Services.GetRequiredService<MqttDispatcher>();
        dispatcher.RegisterHandlers(typeof(MqttHostedService).Assembly);
        return app;
    }
}
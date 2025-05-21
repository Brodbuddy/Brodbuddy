using Application;
using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.Communication.Mail;
using Application.Interfaces.Communication.Notifiers;
using Brodbuddy.WebSocket.State;
using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using Infrastructure.Communication.Mail;
using Infrastructure.Communication.Websocket;
using Application.Interfaces.Communication.Publishers;
using Infrastructure.Communication.Mqtt;
using Infrastructure.Communication.Notifiers;
using Infrastructure.Communication.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Infrastructure.Communication;

public static class Extensions
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services)
    {
        services.AddMail();
        services.AddSocketManager();
        services.AddMqttPublisher();
        services.AddNotifiers();
        return services;
    }

    private static IServiceCollection AddMail(this IServiceCollection services)
    {
        services.AddSingleton<IFluentEmail>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AppOptions>>().Value;
            var email = Email.From(options.Email.FromEmail, options.Email.Sender);
            email.Sender = new MailKitSender(new SmtpClientOptions
            {
                Server = options.Email.Host,
                Port = options.Email.Port
            });
            return email;
        });

        services.AddScoped<IEmailSender, FluentEmailSender>();
   
        return services;
    }


    private static IServiceCollection AddSocketManager(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

        var redisConfig = new ConfigurationOptions
        {
            EndPoints = { appOptions.Dragonfly.ConnectionString },
            AllowAdmin = appOptions.Dragonfly.AllowAdmin,
            AbortOnConnectFail = appOptions.Dragonfly.AbortOnConnectFail
        };
            
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfig));
        services.AddSingleton<ISocketManager, RedisSocketManager>();
        services.AddHostedService<RedisSubscriptionListener>();
        return services;
    }
    
    private static IServiceCollection AddMqttPublisher(this IServiceCollection services)
    {
        services.AddScoped<IMqttPublisher, HiveMqttPublisher>();
        services.AddScoped<IDevicePublisher, TestMqttDevicePublisher>();
        return services;
    }

    private static IServiceCollection AddNotifiers(this IServiceCollection services)
    {
        services.AddScoped<IUserNotifier, WsUserNotifier>();
        return services;
    }
}
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Environments = Application.Environments;

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
        var serviceProvider = services.BuildServiceProvider();
        var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
        
        if (appOptions.Environment == Environments.Production && !string.IsNullOrEmpty(appOptions.Email.SendGridApiKey))
        {
            services.AddFluentEmail(appOptions.Email.FromEmail, appOptions.Email.Sender)
                .AddRazorRenderer()
                .AddSendGridSender(apiKey: appOptions.Email.SendGridApiKey);
        }
        else
        {
            services.AddFluentEmail(appOptions.Email.FromEmail, appOptions.Email.Sender)
                    .AddRazorRenderer()
                    .AddMailKitSender(new SmtpClientOptions
                    {
                        Server = appOptions.Email.Host,
                        Port = appOptions.Email.Port
                    });
        }

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
        services.AddScoped<IOtaPublisher, OtaPublisher>();
        return services;
    }

    private static IServiceCollection AddNotifiers(this IServiceCollection services)
    {
        services.AddScoped<IUserNotifier, WsUserNotifier>();
        return services;
    }
}
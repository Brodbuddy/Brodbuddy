using Application;
using Application.Interfaces.Communication.Mail;
using Application.Interfaces.Communication.Publishers;
using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using Infrastructure.Communication.Mail;
using Infrastructure.Communication.Mqtt;
using Infrastructure.Communication.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Communication;

public static class Extensions
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services)
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
        services.AddMqttPublisher();

        return services;
    }
    
    private static IServiceCollection AddMqttPublisher(this IServiceCollection services)
    {
        services.AddScoped<IMqttPublisher, HiveMqttPublisher>();
        services.AddScoped<IDevicePublisher, MqttDevicePublisher>();
        return services;
    }
}
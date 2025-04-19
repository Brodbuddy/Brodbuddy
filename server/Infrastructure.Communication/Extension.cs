using Application;
using Application.Interfaces;
using Application.Interfaces.Communication.Mail;
using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using Infrastructure.Communication.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Communication;

public static class Extension
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

        return services;
    }
}
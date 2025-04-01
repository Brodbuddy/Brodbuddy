using FluentEmail.MailKitSmtp;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Communication;

public static class Extension
{
    public static IServiceCollection AddCommunicationInfrastructure(this IServiceCollection services)
    {
        services.AddFluentEmail("Test@test.dk", "Jesper")
            .AddMailKitSender(new SmtpClientOptions
            {
                Server = "localhost",
                Port = 1025
            });
        return services;
    }
}
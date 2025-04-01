using Api.Http;
using Application;
using Application.Interfaces;
using Infrastructure.Communication.Mail;
using Infrastructure.Communication;
using Microsoft.Extensions.Options;

namespace Startup;

public class Program
{

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<AppOptions>()
                .BindConfiguration(nameof(AppOptions))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        services.AddCommunicationInfrastructure();
        services.AddHttpApi();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        app.ConfigureHttpApi(appOptions.HttpPort);
    }
    
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        builder.Services.AddScoped<IEmailSender, FluentEmailSender>();
        
        var app = builder.Build();
        
        ConfigureMiddleware(app);

        await app.RunAsync();
    }
}
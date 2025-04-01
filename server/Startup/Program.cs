using Api.Http;
using Application.Interfaces;
using Infrastructure.Communication.Mail;
using Infrastructure.Websocket;

namespace Startup;

public class Program
{

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddCommunicationInfrastructure();
        services.AddHttpApi();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.ConfigureHttpApi(2020);
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
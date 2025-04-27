using Api.Http;
using Application;
using Infrastructure.Communication;
using Infrastructure.Data;
using Microsoft.Extensions.Options;
using Startup.TcpProxy;

namespace Startup;

public static class Program
{
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<AppOptions>()
            .BindConfiguration(nameof(AppOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddCommunicationInfrastructure();
        services.AddDataInfrastructure();
        
        services.AddHttpApi();
        services.AddApplicationServices();
        
        services.AddTcpProxyService();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        app.ConfigureHttpApi(appOptions.Http.Port);
        app.MapGet("/", () => "Hej, nu med multi API :)");
    }

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureServices(builder.Services);

        var app = builder.Build();
        ConfigureMiddleware(app);

        await app.RunAsync();
    }
}
using Api.Http.Extensions;
using Api.Websocket;
using Api.Mqtt;
using Application;
using Infrastructure.Auth;
using Infrastructure.Communication;
using Infrastructure.Data;
using Infrastructure.Monitoring;
using Microsoft.Extensions.Options;
using Serilog;
using LoggerFactory = Infrastructure.Monitoring.LoggerFactory;
using Startup.TcpProxy;

namespace Startup;

public static class Program
{
    private const string ApplicationName = "Brodbuddy";
    
    private static void ConfigureServices(IServiceCollection services, IHostEnvironment environment)
    {
        services.AddOptions<AppOptions>()
            .BindConfiguration(nameof(AppOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddMonitoringInfrastructure(ApplicationName, environment);
        
        services.AddCommunicationInfrastructure();
        services.AddDataInfrastructure();
        services.AddAuthInfrastructure();
        
        services.AddHttpApi();
        services.AddWebsocketApi();
        services.AddMqttApi();
        
        services.AddApplicationServices();
        services.AddTcpProxyService();
    }
    
    private static void ConfigureHost(IHostBuilder host)
    {
        host.AddMonitoringInfrastructure(ApplicationName);
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        app.ConfigureHttpApi(appOptions.Http.Port);
        app.ConfigureWebsocketApi();
        app.ConfigureMqttApi();
        app.ConfigureMonitoringInfrastructure();
        app.MapGet("/", () => "Hej, nu med multi API :)");
    }

    public static async Task Main(string[] args)
    {
        Log.Logger = LoggerFactory.CreateBootstrapLogger();
        Log.Information("Starting {ApplicationName}...", ApplicationName);

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder.Services, builder.Environment);
            ConfigureHost(builder.Host);

            var app = builder.Build();
            ConfigureMiddleware(app);
            
            if (app.Environment.IsDevelopment()) { app.Lifetime.ApplicationStarted.Register(() =>
                {
                    var addresses = app.Urls;
                    if (addresses.Count != 0)
                    {
                        Log.Information("Swagger UI available at: {SwaggerUrl}/swagger", addresses.First());
                    }
                });
            }

            await app.RunAsync();
            Log.Information("{ApplicationName} stopped cleanly", ApplicationName);
        }
        catch (HostAbortedException ex)
        {
            Log.Warning(ex, "{ApplicationName} Host Aborted.", ApplicationName);
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "{ApplicationName} terminated unexpectedly", ApplicationName);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
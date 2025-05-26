using Api.Http.Extensions;
using Api.Websocket;
using Api.Mqtt;
using Api.Websocket.Extensions;
using Api.Websocket.Spec;
using Application;
using Application.Interfaces;
using Application.Interfaces.Data;
using Core.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Communication;
using Infrastructure.Communication.Websocket;
using Infrastructure.Data;
using Infrastructure.Monitoring;
using Microsoft.Extensions.Options;
using Serilog;
using LoggerFactory = Infrastructure.Monitoring.LoggerFactory;
using Startup.Services;
using Startup.TcpProxy;

namespace Startup;

public class Program
{
    private const string ApplicationName = "Brodbuddy";
    private const string GenerateFlag = "--ws";
    
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
        
        services.AddScoped<ISeederService, SeederService>();
    }
    
    private static void TryHandleWebSocketClientGeneration(IServiceCollection services)
    {
        Log.Information("Generating WebSocket TypeScript client...");
        var baseDir = Directory.GetCurrentDirectory();
        var templatesDir = Path.Combine(baseDir, "../Api.Websocket/Spec");
        var outputDir = Path.Combine(baseDir, "../../client/src/api");
        Directory.CreateDirectory(outputDir);
    
        var assemblies = new[]
        {
            typeof(FleckWebSocketServer).Assembly,
            typeof(RedisSocketManager).Assembly,
            typeof(IBroadcastMessage).Assembly,
        };

        var spec = SpecGenerator.GenerateSpec(assemblies, services.BuildServiceProvider());
        TypeScriptGenerator.Generate(spec, templatesDir, outputDir);
        
        const string outputFile = "/client/src/api/websocket-client.ts"; // Skal gerne findes dynamisk i stedet, men nu blir det lige sådan her. 
        Log.Information("WebSocket TypeScript client successfully generated at {OutputFile}", outputFile);
    }
    
    private static void ConfigureHost(IHostBuilder host)
    {
        host.AddMonitoringInfrastructure(ApplicationName);
    }

    private static void LogSwaggerUrl(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                var appOptions = app.Services.GetRequiredService<IOptions<AppOptions>>().Value;
                var publicPort = appOptions.PublicPort;
            
                var publicUrl = $"http://localhost:{publicPort}"; // Burde nok også være dynamisk host i stedet
                Log.Information("Swagger UI available at: {SwaggerUrl}/swagger", publicUrl);
            });
        }
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
            if (args.Contains(GenerateFlag)) TryHandleWebSocketClientGeneration(builder.Services);
            ConfigureHost(builder.Host);

            var app = builder.Build();
            ConfigureMiddleware(app);

            LogSwaggerUrl(app);

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
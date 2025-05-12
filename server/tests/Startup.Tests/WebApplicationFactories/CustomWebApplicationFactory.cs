using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedTestDependencies.Logging;
using Xunit.Abstractions;

namespace Startup.Tests.WebApplicationFactories;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly StartupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    private readonly object _disposalLock = new();
    private bool _isDisposing;
    
    public CustomWebApplicationFactory(StartupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configBuilder =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                // HTTP konfiguration
                {"AppOptions:Http:Port", "5111"},
                
                // Database konfiguration 
                {"AppOptions:Data:ConnectionString", _fixture.Postgres.ConnectionString},
                
                // Redis konfiguration  
                {"AppOptions:Redis:ConnectionString", _fixture.Redis.Container.GetConnectionString()},
                
                // MQTT konfiguration
                {"AppOptions:Mqtt:Host", _fixture.VerneMq.Host},
                {"AppOptions:Mqtt:MqttPort", _fixture.VerneMq.MappedMqttPort.ToString(CultureInfo.InvariantCulture)},
                {"AppOptions:Mqtt:WebSocketPort", _fixture.VerneMq.MappedWebSocketPort.ToString(CultureInfo.InvariantCulture)},
                {"AppOptions:Mqtt:Username", "user"},
                {"AppOptions:Mqtt:Password", "pass"},
            };

            configBuilder.AddInMemoryCollection(testConfig);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new XunitLoggerProvider(_output));
        });
    }
    
    protected override void Dispose(bool disposing)
    {
        lock (_disposalLock)
        {
            if (_isDisposing) return;
            _isDisposing = true;
        }

        try
        {
            if (!disposing) return;
            
            var hostApplicationLifetime = Server.Services.GetService<IHostApplicationLifetime>();
            if (hostApplicationLifetime != null)
            {
                // Stop med at acceptere nye requests
                hostApplicationLifetime.StopApplication();
                    
                // Vi venter lidt for at lukke serveren ordentligt
                Thread.Sleep(100);
            }

            // Vi disposer services i modsatte rækkefølge i forhold til hvordan de blev oprettet.
            var hostedServices = Server.Services.GetServices<IHostedService>().Reverse();
            foreach (var service in hostedServices)
            {
                try
                {
                    if (service is IAsyncDisposable asyncDisposable)
                    {
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }
                    else if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Error disposing service {service.GetType().Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error during factory disposal: {ex.Message}");
        }
        finally
        {
            base.Dispose(disposing);
        }
    }
}
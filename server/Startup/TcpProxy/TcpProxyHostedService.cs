using Application;
using Brodbuddy.TcpProxy.Configuration;
using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Options;

namespace Startup.TcpProxy;

public class TcpProxyHostedService : IHostedService
{
    private const string Localhost = "localhost";
    private readonly AppOptions _options;
    private readonly ILogger<TcpProxyHostedService> _logger;
    private ITcpProxy? _proxy;

    public TcpProxyHostedService(IOptions<AppOptions> options, ILogger<TcpProxyHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting TCP proxy on port {Port}", _options.PublicPort);

        _proxy = new TcpProxyBuilder()
            .WithPublicEndpoint(_options.PublicPort)
            .WithHttpEndpoint(Localhost, _options.Http.Port)
            .WithLogger(_logger)
            .Build();

        return _proxy.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping TCP proxy");
        return _proxy?.StopAsync(cancellationToken) ?? Task.CompletedTask;
    }
}
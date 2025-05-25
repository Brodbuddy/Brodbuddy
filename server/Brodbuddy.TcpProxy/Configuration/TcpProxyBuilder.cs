using Brodbuddy.TcpProxy.Core;
using Brodbuddy.TcpProxy.Interfaces;
using Brodbuddy.TcpProxy.Protocol;
using Brodbuddy.TcpProxy.Routing;
using Microsoft.Extensions.Logging;

namespace Brodbuddy.TcpProxy.Configuration;

public class TcpProxyBuilder
{
    private readonly TcpProxyOptions _options = new();
    private readonly List<IProxyRoute> _routes = [];
    private readonly List<IProtocolDetector> _detectors = [];
    
    public TcpProxyBuilder WithLogger(ILogger logger)
    {
        _options.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }
    
    public TcpProxyBuilder WithPublicEndpoint(int port)
    {
        return WithPublicEndpoint("0.0.0.0", port);
    }
    
    public TcpProxyBuilder WithPublicEndpoint(string host, int port)
    {
        if (string.Equals(host, "public", StringComparison.OrdinalIgnoreCase)) host = "0.0.0.0";
        _options.PublicEndpoint = new Endpoint(host, port);
        return this;
    }

    public TcpProxyBuilder WithHttpEndpoint(string host, int port)
    {
        var endpoint = new Endpoint(host, port);
        _routes.Add(new ProxyRoute("http", endpoint));

        if (!_detectors.Any(d => d is HttpProtocolDetector)) _detectors.Add(new HttpProtocolDetector());

        return this;
    }
    
    public TcpProxyBuilder WithWebSocketEndpoint(string host, int port)
    {
        var endpoint = new Endpoint(host, port);
        _routes.Add(new ProxyRoute("websocket", endpoint));
        
        if (!_detectors.Any(d => d is WebsocketProtocolDetector)) _detectors.Add(new WebsocketProtocolDetector());
            
        return this;
    }

    public TcpProxyBuilder WithCustomEndpoint(string protocolName, string host, int port)
    {
        var endpoint = new Endpoint(host, port);
        _routes.Add(new ProxyRoute(protocolName, endpoint));
        return this;
    }

    public TcpProxyBuilder WithCustomProtocolDetector(IProtocolDetector detector)
    {
        _detectors.Add(detector);
        return this;
    }

    public TcpProxyBuilder WithSslConfiguration(string certificatePath, string password = null!)
    {
        _options.EnableSsl = true;
        _options.CertificatePath = certificatePath;
        _options.CertificatePassword = password;
        return this;
    }

    public ITcpProxy Build()
    {
        var orderedDetectors = new List<IProtocolDetector>();
        
        // WebSocket detection goes first, since HTTP checks for GET, and WS has GET as well
        var wsDetector = _detectors.FirstOrDefault(d => d is WebsocketProtocolDetector);
        if (wsDetector != null) 
        {
            orderedDetectors.Add(wsDetector);
            _options.Logger.LogDebug("Added WebSocket detector first in detection order");
        }

        orderedDetectors.AddRange(_detectors.Where(detector => detector != wsDetector));
        
        orderedDetectors.Add(new DefaultProtocolDetector("http"));
    
        var protocolDetector = new CompositeProtocolDetector(orderedDetectors);
        var connectionManager = new ConnectionManager(_options, _routes);
        return new Core.TcpProxy(_options, protocolDetector, connectionManager);
    }
}
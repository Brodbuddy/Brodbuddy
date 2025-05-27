using Brodbuddy.TcpProxy.Interfaces;

namespace Brodbuddy.TcpProxy.Routing;

public class ProxyRoute : IProxyRoute
{
    public string ProtocolName { get; }
    public IEndpoint DestinationEndpoint { get; }

    public ProxyRoute(string protocolName, IEndpoint destinationEndpoint)
    {
        ProtocolName = protocolName ?? throw new ArgumentNullException(nameof(protocolName));
        DestinationEndpoint = destinationEndpoint ?? throw new ArgumentNullException(nameof(destinationEndpoint));
    }
    
    public bool CanHandleProtocol(string protocolName)
    {
        return string.Equals(ProtocolName, protocolName, StringComparison.OrdinalIgnoreCase);
    }
}
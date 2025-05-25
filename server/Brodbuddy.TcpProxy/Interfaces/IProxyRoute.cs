namespace Brodbuddy.TcpProxy.Interfaces;

public interface IProxyRoute
{
    string ProtocolName { get; }
    IEndpoint DestinationEndpoint { get; }
    bool CanHandleProtocol(string protocolName);
}
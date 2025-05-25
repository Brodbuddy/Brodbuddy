using System.Net;

namespace Brodbuddy.TcpProxy.Interfaces;

public interface IEndpoint
{
    string Host { get; }
    IPAddress IpAddress { get; }
    int Port { get; }
    bool IsSpecified { get; }
}
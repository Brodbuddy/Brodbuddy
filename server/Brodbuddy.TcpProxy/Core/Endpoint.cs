using System.Net;
using Brodbuddy.TcpProxy.Interfaces;

namespace Brodbuddy.TcpProxy.Core;

public class Endpoint : IEndpoint
{
    public string Host { get; }
    public IPAddress IpAddress { get; }
    public int Port { get; }
    public bool IsSpecified => Port > 0;

    public Endpoint(string host, int port)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Port = port;

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1")
            IpAddress = IPAddress.Loopback;
        else if (host == "0.0.0.0" || host.Equals("public", StringComparison.OrdinalIgnoreCase))
            IpAddress = IPAddress.Any;
        else
            IpAddress = IPAddress.Parse(host);
    }

    public Endpoint(IPAddress ipAddress, int port)
    {
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
        Host = ipAddress.ToString();
    }
}
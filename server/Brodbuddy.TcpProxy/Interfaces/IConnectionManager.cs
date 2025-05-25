using System.Net.Sockets;

namespace Brodbuddy.TcpProxy.Interfaces;

public interface IConnectionManager : IAsyncDisposable
{
    Task CreateConnectionAsync(TcpClient clientToProxy, Stream clientStream, string detectedProtocol, CancellationToken cancellationToken = default);
    int ConnectionCount { get; }
}
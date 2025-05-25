using System.Collections.Concurrent;
using System.Net.Sockets;
using Brodbuddy.TcpProxy.Configuration;
using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Logging;

namespace Brodbuddy.TcpProxy.Core;

public class ConnectionManager(TcpProxyOptions options, IEnumerable<IProxyRoute> routes) : IConnectionManager
{
    private readonly IReadOnlyList<IProxyRoute> _routes = routes.ToList() ?? throw new ArgumentNullException(nameof(routes));
    private readonly ConcurrentDictionary<Guid, ConnectionContext> _connections = new();
    private readonly ILogger _logger = options.Logger;

    public int ConnectionCount => _connections.Count;
    
    public async Task CreateConnectionAsync(TcpClient clientToProxy, Stream clientStream, string detectedProtocol, CancellationToken cancellationToken = default)
    {
        var route = _routes.FirstOrDefault(r => r.CanHandleProtocol(detectedProtocol));
        if (route == null)
        {
            _logger.LogWarning("No route found for protocol: {Protocol}", detectedProtocol);
            throw new InvalidOperationException($"No route found for protocol '{detectedProtocol}'");
        }

        var endpoint = route.DestinationEndpoint;
        var connectionId = Guid.NewGuid();
        
        _logger.LogDebug("Creating connection {ConnectionId} for protocol {Protocol} to {Endpoint}", connectionId, detectedProtocol, endpoint);

        var clientFromProxy = new TcpClient();
        try
        {
            await clientFromProxy.ConnectAsync(endpoint.IpAddress, endpoint.Port, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to destination endpoint {Endpoint}", endpoint);
            throw new InvalidOperationException($"Failed to connect to destination endpoint {endpoint}", ex);
        }
        
        var destinationStream = clientFromProxy.GetStream();

        var context = new ConnectionContext(connectionId, clientToProxy, clientFromProxy);
        if (!_connections.TryAdd(connectionId, context))
        {
            _logger.LogWarning("Failed to add connection {ConnectionId} to connection manager", connectionId);
            clientFromProxy.Dispose();
            throw new InvalidOperationException("Could not add connection");
        }

        _logger.LogInformation("Starting bidirectional relay for connection {ConnectionId}", connectionId);
        _ = RelayDataAsync(context, clientStream, destinationStream, "client->server", cancellationToken).ContinueWith(_ => CleanupConnection(connectionId), cancellationToken);
        _ = RelayDataAsync(context, destinationStream, clientStream, "server->client", cancellationToken).ContinueWith(_ => CleanupConnection(connectionId), cancellationToken);
    }

    private void CleanupConnection(Guid connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var context)) return;
        
        try
        {
            context.ServerTcpClient.Dispose();
            _logger.LogDebug("Connection {ConnectionId} resources cleaned up", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up connection {ConnectionId}", connectionId);
        }
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing connection manager with {Count} active connections", _connections.Count);
        
        foreach (var context in _connections.Values)
        {
            try
            {
                context.ClientTcpClient.Dispose();
                context.ServerTcpClient.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing connection {ConnectionId}", context.ConnectionId);
            }
        }

        _connections.Clear();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask; 
    }

    private async Task RelayDataAsync(ConnectionContext context, Stream source, Stream destination, string direction, CancellationToken ct)
    {
        try
        {
            var buffer = new byte[8192];
            _logger.LogDebug("Starting relay {Direction} for connection {ConnectionId}", direction, context.ConnectionId);
        
            while (!ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await source.ReadAsync(buffer, ct);
                    _logger.LogTrace("Relay {Direction}: Read {BytesRead} bytes", direction, bytesRead);
                }
                catch (IOException ex)
                {
                    _logger.LogDebug(ex, "Relay {Direction}: IO exception", direction);
                    break;
                }

                if (bytesRead <= 0)
                {
                    _logger.LogDebug("Relay {Direction}: End of stream", direction);
                    break;
                }

                try
                {
                    await destination.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    _logger.LogTrace("Relay {Direction}: Wrote {BytesRead} bytes", direction, bytesRead);
                }
                catch (IOException ex)
                {
                    _logger.LogDebug(ex, "Relay {Direction}: Write error", direction);
                    break;
                }
            }
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, "Error in relay {Direction} for connection {ConnectionId}", direction, context.ConnectionId);
        }
        finally
        {
            _logger.LogDebug("Relay {Direction} for connection {ConnectionId}: Exiting", direction, context.ConnectionId);
        }
    }
    
    private sealed class ConnectionContext(Guid connectionId, TcpClient clientTcpClient, TcpClient serverTcpClient)
    {
        public Guid ConnectionId { get; } = connectionId;
        public TcpClient ClientTcpClient { get; } = clientTcpClient;
        public TcpClient ServerTcpClient { get; } = serverTcpClient;
    }
}
using System.Net.Sockets;
using Brodbuddy.TcpProxy.Configuration;
using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Logging;

namespace Brodbuddy.TcpProxy.Core;

public class TcpProxy : ITcpProxy
{
    private readonly TcpProxyOptions _options;
    private readonly IProtocolDetector _protocolDetector;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private bool _isDisposed;

    public TcpProxy(TcpProxyOptions options, IProtocolDetector protocolDetector, IConnectionManager connectionManager)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _protocolDetector = protocolDetector ?? throw new ArgumentNullException(nameof(protocolDetector));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = options.Logger;
    }

    public int ConnectionCount => _connectionManager.ConnectionCount;
    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Proxy is already running");
            throw new InvalidOperationException("Proxy is already running");
        }

        if (_isDisposed)
        {
            _logger.LogError("Cannot start disposed proxy");
            throw new ObjectDisposedException(nameof(TcpProxy));
        }

        var publicEndpoint = _options.PublicEndpoint ??
                             throw new InvalidOperationException(
                                 "Public endpoint must be configured before starting the proxy");
        _listener = new TcpListener(publicEndpoint.IpAddress, publicEndpoint.Port);
        _listener.Start();

        _logger.LogInformation("Proxy started on {IpAddress}:{Port}", publicEndpoint.IpAddress, publicEndpoint.Port);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IsRunning = true;
        _listenerTask = AcceptConnectionsAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;
        
        IsRunning = false;
        _logger.LogInformation("Stopping proxy server");
    
        if (_cts != null)
        {
            await _cts.CancelAsync();

            _listener?.Stop();

            if (_listenerTask != null)
            {
                try
                {
                    await _listenerTask;
                }
                catch (OperationCanceledException ex)
                {
                    // Expected if cancellation triggered
                    _logger.LogDebug(ex, "Listener task canceled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping listener");
                }
            
                _listenerTask = null;
            }
        
            _cts.Dispose();
            _cts = null;
        }
        
        _logger.LogInformation("Proxy stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        _logger.LogDebug("Disposing proxy server");

        await StopAsync();
        await _connectionManager.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    private async Task AcceptConnectionsAsync(CancellationToken ct)
    {
        if (_listener == null)
        {
            _logger.LogError("TcpListener was not initialized before attempting to accept connections");
            throw new InvalidOperationException("TcpListener was not initialized");
        }
        
        try
        {
            while (!ct.IsCancellationRequested)
            {
                _logger.LogTrace("Waiting for client connection");
                var client = await _listener.AcceptTcpClientAsync(ct);
                var clientEndpoint = client.Client.RemoteEndPoint;
                _logger.LogDebug("Client connected from {RemoteEndPoint}", clientEndpoint);

                var clientStream = client.GetStream();

                _ = HandleClientAsync(client, clientStream, ct);
            }
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            // Cancellation requested, exit gracefully
            _logger.LogDebug(ex, "Accept connections loop canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting connections");
        }
    }

    private async Task HandleClientAsync(TcpClient client, NetworkStream clientStream, CancellationToken ct)
    {
        var clientEndpoint = client.Client.RemoteEndPoint;
        var buffer = new byte[8192];

        int bytesRead;
        try
        {
            bytesRead = await clientStream.ReadAsync(buffer, ct);
            _logger.LogTrace("Read {BytesRead} bytes from client {RemoteEndPoint}", bytesRead, clientEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Connection closed prematurely from {RemoteEndPoint}", clientEndpoint);
            return; // Connection closed prematurely
        }

        if (bytesRead <= 0)
        {
            _logger.LogDebug("No data received from client {RemoteEndPoint}", clientEndpoint);
            return; // No data
        }

        try
        {
            var protocol = await _protocolDetector.DetectProtocolAsync(buffer.AsMemory(0, bytesRead), ct);
            _logger.LogInformation("Detected protocol: {Protocol} for client {RemoteEndPoint}", protocol,
                clientEndpoint);

            var initialDataStream = new MemoryStream(buffer, 0, bytesRead);
            var compositeStream = new CompositeStream(initialDataStream, clientStream);

            await _connectionManager.CreateConnectionAsync(client, compositeStream, protocol, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client {RemoteEndPoint}", clientEndpoint);
        }
    }
}
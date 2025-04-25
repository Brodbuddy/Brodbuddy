using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using Application;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Fleck;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Websocket;

public class FleckWebSocketServer(WebSocketDispatcher dispatcher, ISocketManager manager, IOptions<AppOptions> options, ILogger<FleckWebSocketServer> logger) : IHostedService , IAsyncDisposable
{
    private WebSocketServer? _server;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting WebSocket server");
        var port = options.Value.WebSocketPort;
        
        if (port <= 0)
        {
            port = FindAvailablePort(options.Value.WebSocketPort);
            logger.LogInformation("No valid port configured, using dynamically assigned port");
        }

        logger.LogInformation("WebSocket server starting on port: {Port}", port);
        _server = new WebSocketServer($"ws://0.0.0.0:{port}");
        ConfigureWebSocket(_server);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping WebSocket server");
        _server?.Dispose();
        return Task.CompletedTask;
    }
    
    private void ConfigureWebSocket(WebSocketServer server)
    {
        server.Start(ws =>
        {
            if (!TryGetClientIdFromConnection(ws, out var clientId)) return;

            ws.OnOpen = async () => await manager.OnOpenAsync(ws, clientId);
            ws.OnClose = async () => await manager.OnCloseAsync(ws, clientId);
            ws.OnMessage = async message => await dispatcher.DispatchAsync(ws, message);
        });
    } 
    
    private bool TryGetClientIdFromConnection(IWebSocketConnection socket, [NotNullWhen(true)] out string? clientId)
    {
        clientId = null;
        var socketId = socket.ConnectionInfo.Id;

        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(socket.ConnectionInfo.Path.Split('?')
                .ElementAtOrDefault(1) ?? "");
            clientId = query["id"];

            if (!string.IsNullOrWhiteSpace(clientId)) return true;
            logger.LogWarning(
                "WebSocket connection rejected: Missing or invalid 'id' query parameter. SocketId: {SocketId}, Path: {Path}, Origin: {Origin}",
                socketId, socket.ConnectionInfo.Path, socket.ConnectionInfo.Origin);
            
            socket.Close(WebSocketStatusCodes.PolicyViolation);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during WebSocket connection acceptance phase for SocketId {SocketId}. Path: {Path}",
                socketId, socket.ConnectionInfo.Path);

            socket.Close(WebSocketStatusCodes.InternalServerError);
            return false;
        }
    }
    
    private static int FindAvailablePort(int preferredPort = 8181)
    {
        if (preferredPort > 0 && IsPortAvailable(preferredPort)) return preferredPort;
    
        var randomListener = new TcpListener(IPAddress.Loopback, 0);
        randomListener.Start();
        var port = ((IPEndPoint)randomListener.LocalEndpoint).Port;
        randomListener.Stop();
        return port;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            tcpListener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_server != null)
        {
            _server.Dispose();
            _server = null;
        }
        await ValueTask.CompletedTask;
        GC.SuppressFinalize(this);
    }
    
}
using System.Net;
using System.Net.Sockets;
using System.Web;
using Application;
using Application.Interfaces.Websocket;
using Brodbuddy.WebSocket.Core;
using Fleck;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Api.Websocket;

public class FleckWebSocketServer(WebSocketDispatcher dispatcher, IConnectionManager connectionManager, IOptions<AppOptions> options, ILogger<FleckWebSocketServer> logger) : IHostedService , IAsyncDisposable
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
            var queryString = ws.ConnectionInfo.Path.Split('?').Length > 1
                ? ws.ConnectionInfo.Path.Split('?')[1]
                : "";

            var id = HttpUtility.ParseQueryString(queryString)["id"] ??
                     throw new Exception("Please specify ID query param for websocket connection");
            
            ws.OnOpen = () => connectionManager.OnOpen(ws, id);
            ws.OnClose = () => connectionManager.OnClose(ws, id);
            ws.OnMessage = async message => await dispatcher.DispatchAsync(ws, message);
        });
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
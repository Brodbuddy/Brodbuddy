using System.Text;
using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brodbuddy.TcpProxy.Protocol;

public class WebsocketProtocolDetector(ILogger? logger = null) : IProtocolDetector
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public ValueTask<string> DetectProtocolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        try
        {
            var str = Encoding.ASCII.GetString(data.Span);
            
            _logger.LogDebug("WebSocket checking: {Headers}", str[..Math.Min(200, str.Length)]);
            
            var hasUpgrade = str.Contains("upgrade", StringComparison.OrdinalIgnoreCase);
            var hasWebSocket = str.Contains("websocket", StringComparison.OrdinalIgnoreCase);
            var hasKey = str.Contains("sec-websocket-key", StringComparison.OrdinalIgnoreCase);

            if (!hasUpgrade || !hasWebSocket || !hasKey) return new ValueTask<string>(string.Empty);
            
            _logger.LogInformation("WebSocket protocol detected");
            return new ValueTask<string>("websocket");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket detection error");
            return new ValueTask<string>(string.Empty);
        }
    }
}
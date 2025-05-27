using Brodbuddy.TcpProxy.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Brodbuddy.TcpProxy.Protocol;

public class CompositeProtocolDetector(IEnumerable<IProtocolDetector> detectors, ILogger? logger = null) : IProtocolDetector
{
    private readonly IReadOnlyList<IProtocolDetector> _detectors = detectors.ToList() ?? throw new ArgumentNullException(nameof(detectors));
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public async ValueTask<string> DetectProtocolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        foreach (var detector in _detectors)
        {
            var protocol = await detector.DetectProtocolAsync(data, cancellationToken);
            if (string.IsNullOrEmpty(protocol)) continue;
            _logger.LogDebug("Protocol detected: {Protocol}", protocol);
            return protocol;
        }
            
        return string.Empty;
    }
}
using Brodbuddy.TcpProxy.Interfaces;

namespace Brodbuddy.TcpProxy.Protocol;

public class DefaultProtocolDetector(string defaultProtocol) : IProtocolDetector
{
    private readonly string _defaultProtocol = defaultProtocol ?? throw new ArgumentNullException(nameof(defaultProtocol));

    public ValueTask<string> DetectProtocolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        return new ValueTask<string>(_defaultProtocol);
    }
}
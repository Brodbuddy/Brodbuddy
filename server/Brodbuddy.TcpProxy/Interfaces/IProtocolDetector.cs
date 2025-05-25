namespace Brodbuddy.TcpProxy.Interfaces;

public interface IProtocolDetector
{
    ValueTask<string> DetectProtocolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
}
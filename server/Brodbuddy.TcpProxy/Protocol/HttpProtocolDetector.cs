using System.Text;
using Brodbuddy.TcpProxy.Interfaces;

namespace Brodbuddy.TcpProxy.Protocol;

public class HttpProtocolDetector : IProtocolDetector
{
    private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "DELETE", "HEAD", "OPTIONS", "TRACE", "CONNECT", "PATCH"];
    
    public ValueTask<string> DetectProtocolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        try
        {
            var str = Encoding.ASCII.GetString(data.Span);

            return HttpMethods.Any(method => str.StartsWith(method + " ", StringComparison.OrdinalIgnoreCase)) 
                ? new ValueTask<string>("http") 
                : new ValueTask<string>(string.Empty);
        }
        catch
        {
            return new ValueTask<string>(string.Empty);
        }
    }
}
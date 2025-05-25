namespace Brodbuddy.TcpProxy.Interfaces;

public interface ITcpProxy : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    int ConnectionCount { get; }
    bool IsRunning { get; }
}
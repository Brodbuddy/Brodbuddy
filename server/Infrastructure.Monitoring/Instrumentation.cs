using System.Diagnostics;

namespace Infrastructure.Monitoring;

public class Instrumentation : IDisposable
{
    private bool _disposed;
    public const string ActivitySourceName = "Brodbuddy";
    public const string Version = "1.0.0";

    public Instrumentation()
    {
        ActivitySource = new ActivitySource(ActivitySourceName, Version);
    }

    public ActivitySource ActivitySource { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) ActivitySource.Dispose();
        
        _disposed = true;
    }
}
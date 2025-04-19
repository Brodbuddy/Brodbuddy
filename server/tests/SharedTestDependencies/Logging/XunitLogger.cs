using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharedTestDependencies.Logging;

public class XunitLogger(ITestOutputHelper output, string category) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        output.WriteLine($"{logLevel} {category}: {message}");
        if (exception != null) output.WriteLine($"Exception: {exception}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

public class LoggerAdapter<T> : ILogger<T>
{
    private readonly ILogger _logger;

    public LoggerAdapter(ILogger logger)
    {
        _logger = logger;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _logger.BeginScope(state);
}

public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;
    private bool _disposed;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ThrowIfDisposed();
        return new XunitLogger(_output, categoryName);
    }

    public ILogger<T> CreateLogger<T>()
    {
        ThrowIfDisposed();
        return new LoggerAdapter<T>(CreateLogger(typeof(T).Name));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(XunitLoggerProvider));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Free any managed objects here
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
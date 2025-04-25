using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTestDependencies.Logging;

public class MessageSinkLoggerAdapter : ILogger
{
    private readonly IMessageSink _sink;
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;

    public MessageSinkLoggerAdapter(IMessageSink sink, string categoryName = "Default", LogLevel minLevel = LogLevel.Information)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _categoryName = categoryName;
        _minLevel = minLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        _sink.OnMessage(new DiagnosticMessage($"[{DateTime.UtcNow:HH:mm:ss.fff} {_categoryName} {logLevel.ToString().ToUpperInvariant()[..4]}] {message}")); 

        if (exception != null)
        {
            _sink.OnMessage(new DiagnosticMessage($"    Exception: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}"));
        }
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        private NullScope() { }
        public void Dispose() { }
    }
}

public static class MessageSinkLoggerExtensions
{
    public static ILogger CreateLogger(
        this IMessageSink sink,
        string categoryName = "Default",
        LogLevel minLevel = LogLevel.Information)
    {
        return new MessageSinkLoggerAdapter(sink, categoryName, minLevel);
    }
}
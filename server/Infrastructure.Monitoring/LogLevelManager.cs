using Application.Interfaces;
using Application.Interfaces.Monitoring;
using Application.Models;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Infrastructure.Monitoring;

public class LogLevelManager : ILogLevelManager
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<LogLevelManager> _logger;
    
    public LogLevelManager(
        LoggingLevelSwitch levelSwitch,
        ILogger<LogLevelManager> logger)
    {
        _levelSwitch = levelSwitch;
        _logger = logger;
    }
    
    public void SetLogLevel(LoggingLevel level)
    {
        var serilogLevel = ConvertToSerilogLevel(level);
        var previousLevel = _levelSwitch.MinimumLevel;

        if (previousLevel == serilogLevel) return;
        _levelSwitch.MinimumLevel = serilogLevel;
        _logger.LogInformation("Log level changed from {PreviousLevel} to {NewLevel}", previousLevel, serilogLevel);
    }
    
    public LoggingLevel GetCurrentLevel() => ConvertFromSerilogLevel(_levelSwitch.MinimumLevel);
    
    private static LogEventLevel ConvertToSerilogLevel(LoggingLevel level) => level switch
    {
        LoggingLevel.Verbose => LogEventLevel.Verbose,
        LoggingLevel.Debug => LogEventLevel.Debug,
        LoggingLevel.Information => LogEventLevel.Information,
        LoggingLevel.Warning => LogEventLevel.Warning,
        LoggingLevel.Error => LogEventLevel.Error,
        LoggingLevel.Fatal => LogEventLevel.Fatal,
        _ => LogEventLevel.Information
    };
    
    private static LoggingLevel ConvertFromSerilogLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => LoggingLevel.Verbose,
        LogEventLevel.Debug => LoggingLevel.Debug,
        LogEventLevel.Information => LoggingLevel.Information,
        LogEventLevel.Warning => LoggingLevel.Warning,
        LogEventLevel.Error => LoggingLevel.Error,
        LogEventLevel.Fatal => LoggingLevel.Fatal,
        _ => LoggingLevel.Information
    };
}
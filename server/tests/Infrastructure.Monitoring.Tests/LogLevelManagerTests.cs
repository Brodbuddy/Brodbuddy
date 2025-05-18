using Application.Models;
using Infrastructure.Monitoring;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace Infrastructure.Monitoring.Tests;

public class LogLevelManagerTests
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly Mock<ILogger<LogLevelManager>> _loggerMock;
    private readonly LogLevelManager _manager;
    
    public LogLevelManagerTests()
    {
        _levelSwitch = new LoggingLevelSwitch();
        _loggerMock = new Mock<ILogger<LogLevelManager>>();
        _manager = new LogLevelManager(_levelSwitch, _loggerMock.Object);
    }
    
    [Fact]
    public void GetCurrentLevel_ReturnsCorrectLevel()
    {
        // Arrange
        _levelSwitch.MinimumLevel = LogEventLevel.Warning;
        
        // Act
        var result = _manager.GetCurrentLevel();
        
        // Assert
        result.ShouldBe(LoggingLevel.Warning);
    }
    
    [Fact]
    public void SetLogLevel_ChangesLevel()
    {
        // Arrange
        _levelSwitch.MinimumLevel = LogEventLevel.Information;
        
        // Act
        _manager.SetLogLevel(LoggingLevel.Debug);
        
        // Assert
        _levelSwitch.MinimumLevel.ShouldBe(LogEventLevel.Debug);
    }
    
    [Fact]
    public void SetLogLevel_LogsChange()
    {
        // Arrange
        _levelSwitch.MinimumLevel = LogEventLevel.Information;
        
        // Act
        _manager.SetLogLevel(LoggingLevel.Debug);
        
        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
    
    [Fact]
    public void SetLogLevel_NoChange_DoesNotLog()
    {
        // Arrange
        _levelSwitch.MinimumLevel = LogEventLevel.Information;
        
        // Act
        _manager.SetLogLevel(LoggingLevel.Information);
        
        // Assert
        _loggerMock.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
    
    [Theory]
    [InlineData(LoggingLevel.Verbose, LogEventLevel.Verbose)]
    [InlineData(LoggingLevel.Debug, LogEventLevel.Debug)]
    [InlineData(LoggingLevel.Information, LogEventLevel.Information)]
    [InlineData(LoggingLevel.Warning, LogEventLevel.Warning)]
    [InlineData(LoggingLevel.Error, LogEventLevel.Error)]
    [InlineData(LoggingLevel.Fatal, LogEventLevel.Fatal)]
    public void LogLevelConversion_MapsCorrectly(LoggingLevel appLevel, LogEventLevel serilogLevel)
    {
        // Act
        _manager.SetLogLevel(appLevel);
        
        // Assert
        _levelSwitch.MinimumLevel.ShouldBe(serilogLevel);
    }
}
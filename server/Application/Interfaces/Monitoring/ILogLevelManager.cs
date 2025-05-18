using Application.Models;

namespace Application.Interfaces.Monitoring;

public interface ILogLevelManager
{
    void SetLogLevel(LoggingLevel level);
    LoggingLevel GetCurrentLevel();
}
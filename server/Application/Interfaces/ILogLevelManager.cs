using Application.Models;

namespace Application.Interfaces;

public interface ILogLevelManager
{
    void SetLogLevel(LoggingLevel level);
    LoggingLevel GetCurrentLevel();
}
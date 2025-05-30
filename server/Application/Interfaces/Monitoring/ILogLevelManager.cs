using Application.Models;
using Core.Enums;

namespace Application.Interfaces.Monitoring;

public interface ILogLevelManager
{
    void SetLogLevel(LoggingLevel level);
    LoggingLevel GetCurrentLevel();
}
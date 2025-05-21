using Core.Enums;

namespace Api.Http.Models;

public record LogLevelResponse(LoggingLevel CurrentLevel);

public record LogLevelUpdateResponse(string Message, LoggingLevel CurrentLevel);
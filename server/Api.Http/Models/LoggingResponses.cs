using Application.Models;

namespace Api.Http.Models;

public record LogLevelResponse(LoggingLevel CurrentLevel);

public record LogLevelUpdateResponse(string Message, LoggingLevel CurrentLevel);
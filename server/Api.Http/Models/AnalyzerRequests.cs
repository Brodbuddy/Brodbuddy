namespace Api.Http.Models;

public record RegisterAnalyzerRequest(
    string ActivationCode,
    string? Nickname = null
);

public record CreateAnalyzerRequest(
    string MacAddress,
    string Name
);
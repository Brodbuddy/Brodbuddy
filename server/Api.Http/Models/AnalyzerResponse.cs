namespace Api.Http.Models;

public record RegisterAnalyzerResponse(
    Guid AnalyzerId,
    string Name,
    string? Nickname,
    bool IsNewAnalyzer,
    bool IsOwner
);

public record AnalyzerListResponse(
    Guid Id,
    string Name,
    string? Nickname,
    DateTime? LastSeen,
    bool IsOwner
);

public record CreateAnalyzerResponse(
    Guid Id,
    string MacAddress,
    string Name,
    string ActivationCode
);
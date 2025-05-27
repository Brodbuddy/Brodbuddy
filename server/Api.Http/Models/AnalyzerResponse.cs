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
    bool IsOwner,
    string? FirmwareVersion,
    bool HasUpdate
);

public record CreateAnalyzerResponse(
    Guid Id,
    string MacAddress,
    string Name,
    string ActivationCode
);

public record AdminAnalyzerListResponse(
    Guid Id,
    string Name,
    string MacAddress,
    string? FirmwareVersion,
    bool IsActivated,
    DateTime? ActivatedAt,
    DateTime? LastSeen,
    DateTime CreatedAt,
    string ActivationCode
);
namespace Application.Models.DTOs;

public record RegisterAnalyzerInput(
    Guid UserId,
    string ActivationCode,
    string? Nickname = null
);
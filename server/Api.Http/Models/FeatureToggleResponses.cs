namespace Api.Http.Models;

public record FeatureToggleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsEnabled,
    int? RolloutPercentage,
    DateTime CreatedAt,
    DateTime? LastModifiedAt
);

public record FeatureToggleListResponse(IEnumerable<FeatureToggleResponse> Features);
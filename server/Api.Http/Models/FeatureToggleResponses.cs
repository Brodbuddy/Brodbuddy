namespace Api.Http.Models;

public record FeatureToggleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsEnabled,
    DateTime CreatedAt,
    DateTime? LastModifiedAt
);

public record FeatureToggleListResponse(IEnumerable<FeatureToggleResponse> Features);
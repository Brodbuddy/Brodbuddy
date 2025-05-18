namespace Api.Http.Models;

public record FeatureToggleUpdateRequest(bool IsEnabled);

public record FeatureToggleRolloutRequest(int Percentage);
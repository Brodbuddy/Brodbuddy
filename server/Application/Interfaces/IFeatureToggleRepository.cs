using Core.Entities;

namespace Application.Interfaces;

public interface IFeatureToggleRepository
{
    public Task<bool> IsEnabledAsync(string featureName);
    public Task<bool> SetEnabledAsync(string featureName, bool enabled);
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId);
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
}
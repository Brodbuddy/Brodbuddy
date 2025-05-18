using Core.Entities;

namespace Application.Interfaces.Data.Repositories;

public interface IFeatureToggleRepository
{
    Task<bool> IsEnabledAsync(string featureName);
    Task<bool> SetEnabledAsync(string featureName, bool enabled);
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId);
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
    Task<bool> AddUserToFeatureAsync(string featureName, Guid userId);
    Task<bool> RemoveUserFromFeatureAsync(string featureName, Guid userId);
    Task<bool> SetRolloutPercentageAsync(string featureName, int percentage);
}
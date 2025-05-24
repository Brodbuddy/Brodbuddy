using Application.Interfaces.Data.Repositories;
using Core.Entities;

namespace Application.Services;

public interface IFeatureToggleService
{
    Task<bool> IsEnabledAsync(string featureName);
    Task<bool> IsEnabledForUserAsync(string featureName, Guid userId);
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
    Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled);
    Task<bool> AddUserToFeatureAsync(string featureName, Guid userId);
    Task<bool> RemoveUserFromFeatureAsync(string featureName, Guid userId);
    Task<bool> SetRolloutPercentageAsync(string featureName, int percentage);
}

public class FeatureToggleService : IFeatureToggleService
{
    private readonly IFeatureToggleRepository _repository;
    
    public FeatureToggleService(IFeatureToggleRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<bool> IsEnabledAsync(string featureName)
    {
        return await _repository.IsEnabledAsync(featureName);
    }

    public async Task<bool> IsEnabledForUserAsync(string featureName, Guid userId)
    {
        return await _repository.IsEnabledForUserAsync(featureName, userId);
    }
    
    public async Task<IEnumerable<Feature>> GetAllFeaturesAsync()
    {
        return await _repository.GetAllFeaturesAsync();
    }
    
    public async Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled)
    {
        return await _repository.SetEnabledAsync(featureName, enabled);
    }
    
    public async Task<bool> AddUserToFeatureAsync(string featureName, Guid userId)
    {
        return await _repository.AddUserToFeatureAsync(featureName, userId);
    }
    
    public async Task<bool> RemoveUserFromFeatureAsync(string featureName, Guid userId)
    {
        return await _repository.RemoveUserFromFeatureAsync(featureName, userId);
    }
    
    public async Task<bool> SetRolloutPercentageAsync(string featureName, int percentage)
    {
        return await _repository.SetRolloutPercentageAsync(featureName, percentage);
    }
}
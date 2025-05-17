using Application.Interfaces;
using Core.Entities;

namespace Application.Services;

public interface IFeatureToggleService
{
    bool IsEnabled(string featureName);
    bool IsEnabledForUser(string featureName, Guid userId);
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
    Task<bool> SetFeatureEnabledAsync(string featureName, bool enabled);
    Task<bool> AddUserToFeatureAsync(string featureName, Guid userId);
    Task<bool> RemoveUserFromFeatureAsync(string featureName, Guid userId);
}

public class FeatureToggleService : IFeatureToggleService
{
    private readonly IFeatureToggleRepository _repository;
    
    public FeatureToggleService(IFeatureToggleRepository repository)
    {
        _repository = repository;
    }
    
    public bool IsEnabled(string featureName)
    {
        return _repository.IsEnabledAsync(featureName).GetAwaiter().GetResult();
    }

    public bool IsEnabledForUser(string featureName, Guid userId)
    {
        return _repository.IsEnabledForUserAsync(featureName, userId).GetAwaiter().GetResult();
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
}
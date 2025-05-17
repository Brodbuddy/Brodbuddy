using Application.Interfaces;

namespace Application.Services;

public interface IFeatureToggleService
{
    /// <summary>
    /// Basal toggle tilstand - f.eks. for API enable/disable
    /// </summary>
    bool IsEnabled(string featureName);
    bool IsEnabledForUser(string featureName, Guid userId);
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
}
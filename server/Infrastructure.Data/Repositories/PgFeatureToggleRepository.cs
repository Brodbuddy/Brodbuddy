using Application.Interfaces;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Data.Repositories;

public class PgFeatureToggleRepository : IFeatureToggleRepository
{
    private readonly PgDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;
    
    private const string CacheKeyPrefix = "feature:";
    
    public PgFeatureToggleRepository(PgDbContext dbContext, IDistributedCache cache, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _cache = cache;
        _timeProvider = timeProvider;
    }
    
    public async Task<bool> IsEnabledAsync(string featureName)
    {
        string cacheKey = $"{CacheKeyPrefix}{featureName}";
        string? cachedValue = await _cache.GetStringAsync(cacheKey);

        if (cachedValue != null) return bool.Parse(cachedValue);

        
        var feature = await _dbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
        if (feature == null) return true;
        
        await _cache.SetStringAsync(
            cacheKey, 
            feature.IsEnabled.ToString(), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
        );
        
        return feature.IsEnabled;
    }

    public async Task<bool> SetEnabledAsync(string featureName, bool enabled)
    {
        var updateResult = await _dbContext.Features
                                           .Where(f => f.Name == featureName)
                                           .ExecuteUpdateAsync(setters => setters
                                               .SetProperty(f => f.IsEnabled, enabled)
                                               .SetProperty(f => f.LastModifiedAt, _timeProvider.Now()));
        
        if (updateResult == 0)
        {
            var feature = new Feature
            {
                Name = featureName,
                IsEnabled = enabled,
                CreatedAt = _timeProvider.Now()
            };
            await _dbContext.Features.AddAsync(feature);
            await _dbContext.SaveChangesAsync();
        }
        
        string cacheKey = $"{CacheKeyPrefix}{featureName}";
        await _cache.RemoveAsync(cacheKey);
    
        return true;
    }

    public async Task<bool> IsEnabledForUserAsync(string featureName, Guid userId)
    {
        string userCacheKey = $"{CacheKeyPrefix}{featureName}:user:{userId}";
        string? cachedValue = await _cache.GetStringAsync(userCacheKey);
        
        if (cachedValue != null) return bool.Parse(cachedValue);
        
        var feature = await _dbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
        if (feature == null) return true; 
        if (!feature.IsEnabled) return false; 
    
        var hasAccess = await _dbContext.FeatureUsers.AnyAsync(fu => fu.FeatureId == feature.Id && fu.UserId == userId);
        
        await _cache.SetStringAsync(
            userCacheKey, 
            hasAccess.ToString(), 
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
        );
        
        return hasAccess;
    }

    public async Task<IEnumerable<Feature>> GetAllFeaturesAsync()
    {
        return await _dbContext.Features.ToListAsync();
    }
    
    public async Task<bool> AddUserToFeatureAsync(string featureName, Guid userId)
    {
        var feature = await _dbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
        if (feature == null) return false;
        
        var existingEntry = await _dbContext.FeatureUsers.FirstOrDefaultAsync(fu => fu.FeatureId == feature.Id && fu.UserId == userId);
            
        if (existingEntry != null) return true;
        
        var featureUser = new FeatureUser
        {
            FeatureId = feature.Id,
            UserId = userId,
            CreatedAt = _timeProvider.Now()
        };
        
        await _dbContext.FeatureUsers.AddAsync(featureUser);
        await _dbContext.SaveChangesAsync();
        
        string userCacheKey = $"{CacheKeyPrefix}{featureName}:user:{userId}";
        await _cache.RemoveAsync(userCacheKey);
        
        return true;
    }
    
    public async Task<bool> RemoveUserFromFeatureAsync(string featureName, Guid userId)
    {
        var feature = await _dbContext.Features.FirstOrDefaultAsync(f => f.Name == featureName);
        if (feature == null) return false;
        
        var featureUser = await _dbContext.FeatureUsers.FirstOrDefaultAsync(fu => fu.FeatureId == feature.Id && fu.UserId == userId);
            
        if (featureUser == null) return false;
        
        _dbContext.FeatureUsers.Remove(featureUser);
        await _dbContext.SaveChangesAsync();
        
        string userCacheKey = $"{CacheKeyPrefix}{featureName}:user:{userId}";
        await _cache.RemoveAsync(userCacheKey);
        
        return true;
    }
}
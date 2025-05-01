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
        await _cache.SetStringAsync(cacheKey, enabled.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    
        return true;
    }

    public async Task<IEnumerable<Feature>> GetAllFeaturesAsync()
    {
        return await _dbContext.Features.ToListAsync();
    }
}
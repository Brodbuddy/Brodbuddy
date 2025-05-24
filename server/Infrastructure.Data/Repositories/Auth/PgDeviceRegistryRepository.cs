using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public class PgDeviceRegistryRepository : IDeviceRegistryRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgDeviceRegistryRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(Guid userId, Guid deviceId, string fingerprint)
    {
        ArgumentException.ThrowIfNullOrEmpty(fingerprint);
        
        var deviceRegistry = new DeviceRegistry
        {
            DeviceId = deviceId,
            UserId = userId,
            Fingerprint = fingerprint,
            CreatedAt = _timeProvider.Now()
        };

        await _dbContext.DeviceRegistries.AddAsync(deviceRegistry);
        await _dbContext.SaveChangesAsync();

        return deviceRegistry.Id;
    }
    
    public async Task<Guid?> GetDeviceIdByFingerprintAsync(Guid userId, string fingerprint)
    {
        ArgumentException.ThrowIfNullOrEmpty(fingerprint);
        
        var deviceRegistry = await _dbContext.DeviceRegistries.Where(dr => dr.UserId == userId && dr.Fingerprint == fingerprint)
                                                              .OrderByDescending(dr => dr.CreatedAt)
                                                              .FirstOrDefaultAsync();
            
        return deviceRegistry?.DeviceId;
    }
    
    public async Task<int> CountByUserIdAsync(Guid userId)
    {
        return await _dbContext.DeviceRegistries.Where(dr => dr.UserId == userId)
                                                .Select(dr => dr.DeviceId)
                                                .Distinct()
                                                .CountAsync();
    }
}
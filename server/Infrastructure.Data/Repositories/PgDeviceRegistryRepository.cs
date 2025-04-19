using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;

namespace Infrastructure.Data.Repositories;

public class PgDeviceRegistryRepository : IDeviceRegistryRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgDeviceRegistryRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(Guid userId, Guid deviceId)
    {
        var deviceRegistry = new DeviceRegistry
        {
            DeviceId = deviceId,
            UserId = userId,
            CreatedAt = _timeProvider.Now()
        };

        await _dbContext.DeviceRegistries.AddAsync(deviceRegistry);
        await _dbContext.SaveChangesAsync();

        return deviceRegistry.Id;
    }
}
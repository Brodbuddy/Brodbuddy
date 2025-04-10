using Application.Interfaces;
using Core.Entities;

namespace Infrastructure.Data.Postgres;

public class PostgresDeviceRegistryRepository : IDeviceRegistryRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresDeviceRegistryRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(Guid userId, Guid deviceId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var deviceRegistry = new DeviceRegistry
        {
            DeviceId = deviceId,
            UserId = userId,
            CreatedAt = now
        };

        await _dbContext.DeviceRegistries.AddAsync(deviceRegistry);
        await _dbContext.SaveChangesAsync();

        return deviceRegistry.Id;
    }
}
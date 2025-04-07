using Application.Interfaces;
using Core.Entities;

namespace Infrastructure.Data.Postgres;

public class PostgresDeviceRepository : IDeviceRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresDeviceRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(Device device)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
       
        try
        {
            if (device.Id == Guid.Empty)
            {
                device.CreatedAt = now;
                device.LastSeenAt = now;
                device.IsActive = true;

                await _dbContext.Devices.AddAsync(device);
            }

            await _dbContext.SaveChangesAsync();
            return device.Id;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to save device", ex);
        }
}

    public Task<Device> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Device>> Get(IEnumerable<Guid> ids)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateLastSeenAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DisableAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

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
        if (device == null) throw new NullReferenceException("Device cannot be null");

        if (device.Id != Guid.Empty) throw new ArgumentException();
        
        if (device.Id == Guid.Empty)
        {
            device.CreatedAt = _timeProvider.Now();
            device.LastSeenAt = _timeProvider.Now();
            device.IsActive = true;

            await _dbContext.Devices.AddAsync(device);
        }

        await _dbContext.SaveChangesAsync();
        return device.Id;
     
    }

    public async Task<Device> GetAsync(Guid id)
    {
        return await _dbContext.Devices
                   .FirstOrDefaultAsync(r => r.Id == id)
               ?? throw new ArgumentException($"Device with ID {id} not found");
    }

    public async Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await _dbContext.Devices
            .Where(d => ids.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbContext.Devices.AnyAsync(d => d.Id == id);
    }

    public async Task<bool> UpdateLastSeenAsync(Guid id, DateTime lastSeenTime)
    {
        var rowsAffected = await _dbContext.Devices
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(d => d.LastSeenAt, lastSeenTime));
        
        return rowsAffected > 0;
    }

    public async Task<bool> DisableAsync(Guid id)
    {
        var rowsAffected = await _dbContext.Devices
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(d => d.IsActive, false));

        return rowsAffected > 0;
    }
}
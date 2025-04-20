using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgDeviceRepository : IDeviceRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgDeviceRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(Device device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (device.Id != Guid.Empty) throw new ArgumentException("Device ID should be empty", nameof(device));

        var now = _timeProvider.Now();
        device.CreatedAt = now;
        device.LastSeenAt = now;
        device.IsActive = true;

        await _dbContext.Devices.AddAsync(device);
        await _dbContext.SaveChangesAsync();
        return device.Id;
    }

    public async Task<Device> GetAsync(Guid id)
    {
        return await _dbContext.Devices.FirstOrDefaultAsync(r => r.Id == id) ?? throw new ArgumentException($"Device with ID {id} not found");
    }

    public async Task<IEnumerable<Device>> GetByIdsAsync(IEnumerable<Guid> ids)
    { 
        ArgumentNullException.ThrowIfNull(ids);
        return await _dbContext.Devices.Where(d => ids.Contains(d.Id)).ToListAsync();
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
using Application.Interfaces.Data.Repositories;
using Application.Models;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgTelemetryRepository : ITelemetryRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgTelemetryRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveReadingAsync(string deviceId, double distance, double risePercentage, DateTime timestamp)
    {
        var entity = new DeviceTelemetry
        {
            DeviceId = deviceId,
            Distance = distance,
            RisePercentage = risePercentage,
            Timestamp = timestamp,
            CreatedAt = _timeProvider.Now()
        };

        await _dbContext.DeviceTelemetries.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<IEnumerable<TelemetryReading>> GetAllReadingsAsync(int limit = 100)
    {
        return await _dbContext.DeviceTelemetries
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Select(t => new TelemetryReading
            {
                DeviceId = t.DeviceId,
                Distance = t.Distance,
                RisePercentage = t.RisePercentage,
                Timestamp = t.Timestamp
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<TelemetryReading>> GetReadingsByDeviceAsync(string deviceId, int limit = 100)
    {
        return await _dbContext.DeviceTelemetries
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit) 
            .Select(t => new TelemetryReading
            {
                DeviceId = t.DeviceId,
                Distance = t.Distance,
                RisePercentage = t.RisePercentage,
                Timestamp = t.Timestamp
            })
            .ToListAsync();
    }
}
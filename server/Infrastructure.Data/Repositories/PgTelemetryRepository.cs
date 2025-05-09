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

    public async Task<Guid> SaveReadingAsync(TelemetryReading telemetryReading)
    {
        var entity = new DeviceTelemetry
        {
            DeviceId = telemetryReading.DeviceId!,
            Distance = telemetryReading.Distance,
            RisePercentage = telemetryReading.RisePercentage,
            Timestamp = telemetryReading.Timestamp,
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
            (
                t.DeviceId,
                t.Distance,
                t.RisePercentage,
                t.Timestamp
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<TelemetryReading>> GetReadingsByDeviceAsync(string deviceId, int limit = 100)
    {
        return await _dbContext.DeviceTelemetries
            .Where(t => t.DeviceId == deviceId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit) 
            .Select(t => new TelemetryReading
            (
                t.DeviceId,
                t.Distance,
                t.RisePercentage,
                t.Timestamp
            ))
            .ToListAsync();
    }
}
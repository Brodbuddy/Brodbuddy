using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgFirmwareRepository : IFirmwareRepository
{
    private readonly PgDbContext _context;
    private readonly TimeProvider _timeProvider;

    public PgFirmwareRepository(PgDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<FirmwareVersion?> GetByIdAsync(Guid id)
    {
        return await _context.FirmwareVersions
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<FirmwareVersion>> GetAllAsync()
    {
        return await _context.FirmwareVersions
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<FirmwareVersion> CreateAsync(FirmwareVersion firmware)
    {
        firmware.CreatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _context.FirmwareVersions.Add(firmware);
        await _context.SaveChangesAsync();
        return firmware;
    }

    public async Task<FirmwareVersion?> GetLatestStableAsync()
    {
        return await _context.FirmwareVersions
            .Where(f => f.IsStable)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<FirmwareUpdate> CreateUpdateAsync(FirmwareUpdate update)
    {
        update.StartedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _context.FirmwareUpdates.Add(update);
        await _context.SaveChangesAsync();
        return update;
    }

    public async Task UpdateUpdateStatusAsync(Guid updateId, string status, int? progress = null)
    {
        var update = await _context.FirmwareUpdates.FindAsync(updateId);
        if (update != null)
        {
            update.Status = status;
            if (progress.HasValue)
                update.Progress = progress.Value;
            
            if (status == FirmwareUpdate.OtaStatus.Complete || status == FirmwareUpdate.OtaStatus.Failed)
                update.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
                
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<FirmwareUpdate?> GetActiveUpdateForAnalyzerAsync(Guid analyzerId)
    {
        return await _context.FirmwareUpdates
            .Where(u => u.AnalyzerId == analyzerId && 
                   (u.Status == FirmwareUpdate.OtaStatus.Started || 
                    u.Status == FirmwareUpdate.OtaStatus.Downloading || 
                    u.Status == FirmwareUpdate.OtaStatus.Applying))
            .OrderByDescending(u => u.StartedAt)
            .FirstOrDefaultAsync();
    }
}
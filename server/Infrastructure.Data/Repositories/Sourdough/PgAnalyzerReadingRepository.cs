using Application.Interfaces.Data.Repositories.Sourdough;
using Core.Entities;
using Core.ValueObjects;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Sourdough;

public class PgAnalyzerReadingRepository : IAnalyzerReadingRepository
{
    
    private readonly PgDbContext _context;
    
    public PgAnalyzerReadingRepository(PgDbContext context)
    {
        _context = context;
    }
    
    public async Task SaveReadingAsync(SourdoughReading reading, Guid userId, Guid analyzerId)
    {
        bool exists = await _context.AnalyzerReadings.AnyAsync(r => 
            r.AnalyzerId == analyzerId && r.EpochTime == reading.EpochTime);
    
        if (!exists)
        {
            var analyzerReading = new AnalyzerReading
            {
                AnalyzerId = analyzerId,
                Temperature = (decimal?)reading.Temperature, 
                Humidity = (decimal?)reading.Humidity, 
                Rise = (decimal?)reading.Rise,
                EpochTime = reading.EpochTime,
                Timestamp = reading.Timestamp,
                LocalTime = reading.LocalTime,
                UserId = userId
            };
        
            _context.AnalyzerReadings.Add(analyzerReading);
            await _context.SaveChangesAsync();
        }
    }
    
  
    public async Task<IEnumerable<AnalyzerReading>> GetReadingsAsync(Guid analyzerId, DateTime? from = null, DateTime? toDate = null)
    {
        var fromUnspecified = from.HasValue ? DateTime.SpecifyKind(from.Value, DateTimeKind.Unspecified) : (DateTime?)null;
        var toDateUnspecified = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value, DateTimeKind.Unspecified) : (DateTime?)null;

        var query = _context.AnalyzerReadings
            .Where(ar => ar.AnalyzerId == analyzerId);

        if (fromUnspecified.HasValue)
            query = query.Where(ar => ar.LocalTime >= fromUnspecified.Value);

        if (toDateUnspecified.HasValue)
            query = query.Where(ar => ar.LocalTime <= toDateUnspecified.Value);

        return await query.ToListAsync();
    }
    
    
    public async Task<AnalyzerReading?> GetLatestReadingAsync(Guid analyzerId)
    {
        return await _context.AnalyzerReadings
            .Where(ar => ar.AnalyzerId == analyzerId)
            .OrderByDescending(ar => ar.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<AnalyzerReading>> GetLatestReadingsForCachingAsync(
        Guid analyzerId, 
        int maxResults = 500)
    {
        return await _context.AnalyzerReadings
            .Where(r => r.AnalyzerId == analyzerId)
            .OrderByDescending(r => r.Timestamp)
            .Take(maxResults)
            .ToListAsync();
    }
    
}
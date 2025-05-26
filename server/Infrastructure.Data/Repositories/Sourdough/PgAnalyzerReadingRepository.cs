using Application.Interfaces.Data.Repositories.Sourdough;
using Core.Entities;
using Core.ValueObjects;
using Infrastructure.Data.Persistence;

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
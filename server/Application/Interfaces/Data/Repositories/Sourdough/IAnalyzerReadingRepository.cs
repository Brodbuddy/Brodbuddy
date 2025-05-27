using Core.Entities;
using Core.ValueObjects;

namespace Application.Interfaces.Data.Repositories.Sourdough;

public interface IAnalyzerReadingRepository
{
    Task SaveReadingAsync(SourdoughReading reading, Guid userId, Guid analyzerId);
    Task<IEnumerable<AnalyzerReading>> GetReadingsAsync(Guid analyzerId, DateTime? from = null, DateTime? toDate = null);
    Task<AnalyzerReading?> GetLatestReadingAsync(Guid analyzerId);
    Task<IEnumerable<AnalyzerReading>> GetLatestReadingsForCachingAsync(Guid analyzerId, int maxResults = 500);
}
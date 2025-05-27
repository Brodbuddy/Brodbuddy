using Application.Interfaces.Data.Repositories.Sourdough;
using Core.Entities;


namespace Application.Services.Sourdough;

public class SourdoughReadingsService
{
    private readonly IAnalyzerReadingRepository _repository;

    public SourdoughReadingsService(IAnalyzerReadingRepository repository)
    {
        _repository = repository;
    }

    public async Task<AnalyzerReading?> GetLatestReadingAsync(Guid analyzerId)
    {
        return await _repository.GetLatestReadingAsync(analyzerId);
    }

    public async Task<IEnumerable<AnalyzerReading>> GetLatestReadingsForCachingAsync(Guid analyzerId, int maxResults)
    {
        return await _repository.GetLatestReadingsForCachingAsync(analyzerId, maxResults);
    }

    public async Task<IEnumerable<AnalyzerReading>> GetReadingsByTimeRangeAsync(Guid analyzerId, DateTime? from, DateTime? toDate)
    {
        return await _repository.GetReadingsAsync(analyzerId, from, toDate);
    }
}
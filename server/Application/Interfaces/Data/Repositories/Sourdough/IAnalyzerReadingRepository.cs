using Core.ValueObjects;

namespace Application.Interfaces.Data.Repositories.Sourdough;

public interface IAnalyzerReadingRepository
{
    Task SaveReadingAsync(SourdoughReading reading, Guid userId, Guid analyzerId);
}
using Core.Entities;

namespace Application.Interfaces.Data.Repositories.Sourdough;

public interface IUserAnalyzerRepository
{
    Task<Guid> SaveAsync(UserAnalyzer userAnalyzer);
}
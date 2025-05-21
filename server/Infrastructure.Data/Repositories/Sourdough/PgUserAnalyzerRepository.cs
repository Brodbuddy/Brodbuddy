using Application.Interfaces.Data.Repositories.Sourdough;
using Core.Entities;
using Infrastructure.Data.Persistence;

namespace Infrastructure.Data.Repositories.Sourdough;

public class PgUserAnalyzerRepository : IUserAnalyzerRepository
{
    private readonly PgDbContext _context;

    public PgUserAnalyzerRepository(PgDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> SaveAsync(UserAnalyzer userAnalyzer)
    {
        _context.UserAnalyzers.Add(userAnalyzer);
        await _context.SaveChangesAsync();
        return userAnalyzer.Id;
    }
}
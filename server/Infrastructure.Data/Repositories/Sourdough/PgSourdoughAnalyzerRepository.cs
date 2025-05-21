using Application.Interfaces.Data.Repositories.Sourdough;
using Core.Entities;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Sourdough;

public class PgSourdoughAnalyzerRepository : ISourdoughAnalyzerRepository
{
    private readonly PgDbContext _context;

    public PgSourdoughAnalyzerRepository(PgDbContext context)
    {
        _context = context;
    }

    public async Task<SourdoughAnalyzer?> GetByMacAddressAsync(string macAddress)
    {
        return await _context.SourdoughAnalyzers.FirstOrDefaultAsync(a => a.MacAddress == macAddress);
    }

    public async Task<SourdoughAnalyzer?> GetByActivationCodeAsync(string activationCode)
    {
        return await _context.SourdoughAnalyzers.Include(a => a.UserAnalyzers)
                                                .FirstOrDefaultAsync(a => a.ActivationCode == activationCode);
    }

    public async Task<Guid> SaveAsync(SourdoughAnalyzer analyzer)
    {
        _context.SourdoughAnalyzers.Add(analyzer);
        await _context.SaveChangesAsync();
        return analyzer.Id;
    }

    public async Task<IEnumerable<SourdoughAnalyzer>> GetByUserIdAsync(Guid userId)
    {
        return await _context.SourdoughAnalyzers.Include(a => a.UserAnalyzers)
                                                .Where(a => a.UserAnalyzers.Any(ua => ua.UserId == userId))
                                                .ToListAsync();
    }
}
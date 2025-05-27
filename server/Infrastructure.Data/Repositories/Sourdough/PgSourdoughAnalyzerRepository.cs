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

    public async Task<IEnumerable<SourdoughAnalyzer>> GetAllAsync()
    {
        return await _context.SourdoughAnalyzers.Include(a => a.UserAnalyzers)
                                                .ToListAsync();
    }

    public async Task<SourdoughAnalyzer?> GetByIdAsync(Guid id)
    {
        return await _context.SourdoughAnalyzers.Include(a => a.UserAnalyzers)
                                                .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task UpdateAsync(SourdoughAnalyzer analyzer)
    {
        _context.SourdoughAnalyzers.Update(analyzer);
        await _context.SaveChangesAsync();
    }

    public async Task<Guid?> GetOwnersUserIdAsync(Guid analyzerId)
    {
        var userAnalyzer = await _context.UserAnalyzers
            .FirstOrDefaultAsync(ua => ua.AnalyzerId == analyzerId && ua.IsOwner == true);
    
        return userAnalyzer?.UserId;
    }
}
using Application.Interfaces.Data.Repositories.Auth;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories.Auth;

public class PgUserIdentityRepository : IUserIdentityRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgUserIdentityRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(string email)
    {
        var now = _timeProvider.Now();
        var user = new User
        {
            Email = email,
            CreatedAt = now
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return user.Id;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _dbContext.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _dbContext.Users.AnyAsync(u => EF.Functions.ILike(u.Email, email.Trim()));
    }

    public async Task<User?> GetAsync(Guid id)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetAsync(string email)
    {
        return await _dbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email.Trim()));
    }
}
using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Core.Validation;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

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
        if (!ValidationRules.IsValidEmail(email)) throw new ArgumentException("Invalid email format", nameof(email));
        
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
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        return await _dbContext.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> ExistsAsync(string email)
    {
        if (!ValidationRules.IsValidEmail(email)) throw new ArgumentException("Invalid email format", nameof(email));
        return await _dbContext.Users.AnyAsync(u => EF.Functions.ILike(u.Email, email.Trim()));
    }

    public async Task<User?> GetAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(id));
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetAsync(string email)
    {
        if (!ValidationRules.IsValidEmail(email)) throw new ArgumentException("Invalid email format", nameof(email));
        return await _dbContext.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Email, email.Trim()));
    }
}
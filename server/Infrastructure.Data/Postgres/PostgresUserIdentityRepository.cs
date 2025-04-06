using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Postgres;

public class PostgresUserIdentityRepository : IUserIdentityRepository
{

    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresUserIdentityRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveAsync(string email)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Email = email,
            RegisterDate = now
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
        return await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == email.Trim().ToLowerInvariant());
    }

    public async Task<User> GetAsync(Guid id)
    {
        return await _dbContext.Users
           .FirstOrDefaultAsync(u => u.Id == id)
           ?? throw new KeyNotFoundException($"User with ID {id} not found");
    }

    public async Task<User> GetAsync(string email)
    {
        return await _dbContext.Users
           .FirstOrDefaultAsync(u => u.Email.ToLower() == email.Trim().ToLowerInvariant())
           ?? throw new KeyNotFoundException($"User with email {email} not found");
    }
}
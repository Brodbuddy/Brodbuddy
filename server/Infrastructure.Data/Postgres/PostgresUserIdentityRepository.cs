using Application.Interfaces;
using Core.Entities;

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

    public async Task<Guid> CreateAsync(string email)
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

    public Task<bool> ExistsAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<User> GetAsync(string email)
    {
        throw new NotImplementedException();
    }
}
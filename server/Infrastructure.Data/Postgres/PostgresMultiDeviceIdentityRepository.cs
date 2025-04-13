using Application.Interfaces;
using Core.Entities;

namespace Infrastructure.Data.Postgres;

public class PostgresMultiDeviceIdentityRepository : IMultiDeviceIdentityRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PostgresMultiDeviceIdentityRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshTokenId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var tokenContext = new TokenContext
        {
            UserId = userId,
            DeviceId = deviceId,
            RefreshTokenId = refreshTokenId,
            CreatedAt = now,
            IsRevoked = false
        };

        await _dbContext.TokenContexts.AddAsync(tokenContext);
        await _dbContext.SaveChangesAsync();
    }
}
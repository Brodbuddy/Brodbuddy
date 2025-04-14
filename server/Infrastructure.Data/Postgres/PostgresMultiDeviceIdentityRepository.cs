using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<bool> RevokeTokenContextAsync(Guid refreshTokenId)
    {
        var tokenContext = await _dbContext.TokenContexts
            .FirstOrDefaultAsync(tc => tc.RefreshTokenId == refreshTokenId);

        if (tokenContext == null)
        {
            return false;
        }

        tokenContext.IsRevoked = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }


    public async Task<TokenContext?> GetTokenContextByRefreshTokenIdAsync(Guid refreshTokenId)
    {
        return await _dbContext.TokenContexts
            .Include(tc => tc.User)
            .Include(tc => tc.Device)
            .Include(tc => tc.RefreshToken)
            .FirstOrDefaultAsync(tc => tc.RefreshTokenId == refreshTokenId && !tc.IsRevoked);
    }

  
}
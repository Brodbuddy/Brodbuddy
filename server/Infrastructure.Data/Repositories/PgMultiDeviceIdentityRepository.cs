using Application.Interfaces;
using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgMultiDeviceIdentityRepository : IMultiDeviceIdentityRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PgMultiDeviceIdentityRepository(PgDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Guid> SaveIdentityAsync(Guid userId, Guid deviceId, Guid refreshTokenId)
    {
        var tokenContext = new TokenContext
        {
            UserId = userId,
            DeviceId = deviceId,
            RefreshTokenId = refreshTokenId,
            CreatedAt = _timeProvider.Now(),
            IsRevoked = false
        };

        await _dbContext.TokenContexts.AddAsync(tokenContext);
        await _dbContext.SaveChangesAsync();

        return tokenContext.Id;
    }

    public async Task<bool> RevokeTokenContextAsync(Guid refreshTokenId)
    {
        int rowsAffected = await _dbContext.TokenContexts
            .Where(tc => tc.RefreshTokenId == refreshTokenId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tc => tc.IsRevoked, true));

        return rowsAffected > 0;
    }


    public async Task<TokenContext?> GetAsync(Guid refreshTokenId)
    {
        return await _dbContext.TokenContexts
            .Include(tc => tc.User)
            .Include(tc => tc.Device)
            .Include(tc => tc.RefreshToken)
            .FirstOrDefaultAsync(tc => tc.RefreshTokenId == refreshTokenId && !tc.IsRevoked);
    }
}
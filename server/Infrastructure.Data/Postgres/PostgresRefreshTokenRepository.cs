using System.Security.Cryptography;
using Application.Interfaces;
using Core.Entities;
using Core.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Postgres;

public class PostgresRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PostgresDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    
    public PostgresRefreshTokenRepository(PostgresDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }
    
    
    public async Task<(string token, Guid tokenId)> CreateAsync(string token, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            CreatedAt = _timeProvider.Now(),
            ExpiresAt = expiresAt
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        return (refreshToken.Token, refreshToken.Id);
    }

    public async Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token)
    {
        var now = _timeProvider.Now();

        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken == null)
            return (false, Guid.Empty);

        if (refreshToken.RevokedAt != null)
            return (false, Guid.Empty);

        return refreshToken.ExpiresAt < now ? (false, Guid.Empty) : (true, refreshToken.Id);
    }

    public async Task<bool> RevokeAsync(Guid tokenId)
    {
        var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == tokenId);

        if (refreshToken == null)
            return false;

        refreshToken.RevokedAt = _timeProvider.Now();

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<(string token, Guid tokenId)> RotateAsync(Guid oldTokenId)
    {
        var oldToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == oldTokenId);

        if (oldToken == null) throw new InvalidOperationException("Old token not found");
        
        var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = _timeProvider.Now();
        var expiresAt = now.AddDays(30);
        
        var refreshToken = new RefreshToken
        {
            Token = newToken,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken);
        await _dbContext.SaveChangesAsync();

        oldToken.RevokedAt = now;
        oldToken.ReplacedByTokenId = refreshToken.Id;

        await _dbContext.SaveChangesAsync();

        return (refreshToken.Token, refreshToken.Id);
    }
}
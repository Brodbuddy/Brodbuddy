using System.Security.Cryptography;
using Application.Interfaces;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Postgres;

public class RefreshTokenRepository(PostgresDbContext dbcontext, TimeProvider timeProvider) : IRefreshTokenRepository
{
    public async Task<Guid> CreateAsync(string token, DateTime expiresAt)
    {
        
        var refreshToken = new RefreshToken
        {
            Token = token,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = expiresAt
        };
        
        await dbcontext.RefreshTokens.AddAsync(refreshToken);
        await dbcontext.SaveChangesAsync();

        return refreshToken.Id;

    }

    public async Task<(bool isValid, Guid tokenId)> TryValidateAsync(string token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        
        var refreshToken = await dbcontext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
        
        if (refreshToken == null)
            return (false, Guid.Empty);
        
        if (refreshToken.RevokedAt != null)
            return (false, Guid.Empty);
        
        return refreshToken.ExpiresAt < now ? (false, Guid.Empty) : (true, refreshToken.Id);
    }

    public async Task<bool> RevokeAsync(Guid tokenId)
    {
        var refreshToken = await dbcontext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == tokenId);
    
        if (refreshToken == null)
            return false;
        
        refreshToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
    
        await dbcontext.SaveChangesAsync();
        return true;
    }

    public async Task<string> RotateAsync(Guid oldTokenId)
    {
        var oldToken = await dbcontext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Id == oldTokenId);
    
        if (oldToken == null)
            throw new InvalidOperationException("Old token not found");
    
       
        var newToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddDays(30); 
    
        
        var refreshToken = new RefreshToken
        {
            Token = newToken,
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
    
        await dbcontext.RefreshTokens.AddAsync(refreshToken);
        
        oldToken.RevokedAt = now;
        oldToken.ReplacedByTokenId = refreshToken.Id;
    
        await dbcontext.SaveChangesAsync();
    
        return newToken;
    }
}
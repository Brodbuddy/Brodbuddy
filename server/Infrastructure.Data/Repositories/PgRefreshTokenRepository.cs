using System.Security.Cryptography;
using System.Transactions;
using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class PgRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly PgDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    
    public PgRefreshTokenRepository(PgDbContext dbContext,  TimeProvider timeProvider)
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
        var now = _timeProvider.Now(); 

        int rowsAffected = await _dbContext.RefreshTokens
            .Where(rt => rt.Id == tokenId && rt.RevokedAt == null ) 
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAt, now));

        return rowsAffected > 0;
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
        
        if (_dbContext.Database.CurrentTransaction != null)
        {
            await _dbContext.RefreshTokens.AddAsync(refreshToken);
            await _dbContext.SaveChangesAsync();

            oldToken.RevokedAt = now;
            oldToken.ReplacedByTokenId = refreshToken.Id;

            await _dbContext.SaveChangesAsync(); 
            return (refreshToken.Token, refreshToken.Id);
        }
        
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                await _dbContext.RefreshTokens.AddAsync(refreshToken);
                await _dbContext.SaveChangesAsync();

                oldToken.RevokedAt = now;
                oldToken.ReplacedByTokenId = refreshToken.Id;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Failed to rotate refresh token", ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Unexpected error while rotating refresh token", ex);
            }
        }
        
        return (refreshToken.Token, refreshToken.Id);
    }
}
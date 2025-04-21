using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Tests.Database;

public static class TestDataSeeder
{
    public static async Task<User> SeedUserAsync(this PgDbContext context,
        TimeProvider timeProvider,
        string email = "peter@test.dk")
    {
        var user = new User
        {
            Email = email,
            CreatedAt = timeProvider.Now()
        };
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return user;
    }

    public static async Task<Device> SeedDeviceAsync(this PgDbContext context, TimeProvider timeProvider,
        string os = "linux", string browser = "firefox", DateTime? lastSeenAt = null, bool isActive = true)
    {
        var now = timeProvider.Now();
        var device = new Device
        {
            Name = $"{browser}_{os}",
            Os = os,
            Browser = browser,
            CreatedAt = now,
            LastSeenAt = lastSeenAt ?? now,
            IsActive = isActive
        };
        await context.Devices.AddAsync(device);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return device;
    }

    public static async Task<RefreshToken> SeedRefreshTokenAsync(this PgDbContext context,
        TimeProvider timeProvider, int expiresDays = 30, string tokenValue = "test-refresh-token", DateTime? revokedAt = null, Guid? replacedByTokenId = null)
    {
        var now = timeProvider.Now();
        var refreshToken = new RefreshToken
        {
            Token = tokenValue,
            CreatedAt = now,
            ExpiresAt = now.AddDays(expiresDays),
            RevokedAt = revokedAt,
            ReplacedByTokenId = replacedByTokenId
        };
        await context.RefreshTokens.AddAsync(refreshToken);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return refreshToken;
    }

    public static async Task<OneTimePassword> SeedOtpAsync(this PgDbContext context, TimeProvider timeProvider,
        int expiresMinutes = 15, int code = 123456, bool isUsed = false)
    {
        var now = timeProvider.Now();
        var otp = new OneTimePassword
        {
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(expiresMinutes),
            IsUsed = isUsed
        };
        await context.OneTimePasswords.AddAsync(otp);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        var createdOtp = await context.OneTimePasswords
            .AsNoTracking()
            .FirstAsync(o => o.Id == otp.Id); 
        return createdOtp;
    }

    public static async Task<TokenContext> SeedTokenContextAsync(
        this PgDbContext context,
        TimeProvider timeProvider,
        Guid? userId = null,
        Guid? deviceId = null,
        Guid? refreshTokenId = null,
        bool isRevoked = false)
    {
        var effectiveUserId = userId ?? (await context.SeedUserAsync(timeProvider)).Id;
        var effectiveDeviceId = deviceId ?? (await context.SeedDeviceAsync(timeProvider)).Id;
        var effectiveRefreshTokenId = refreshTokenId ?? (await context.SeedRefreshTokenAsync(timeProvider)).Id;

        var tokenContext = new TokenContext
        {
            UserId = effectiveUserId,
            DeviceId = effectiveDeviceId,
            RefreshTokenId = effectiveRefreshTokenId,
            CreatedAt = timeProvider.Now(),
            IsRevoked = isRevoked
        };

        await context.TokenContexts.AddAsync(tokenContext);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var createdContext = await context.TokenContexts
            .AsNoTracking()
            .FirstAsync(tc => tc.UserId == effectiveUserId &&
                              tc.DeviceId == effectiveDeviceId &&
                              tc.RefreshTokenId == effectiveRefreshTokenId);
        return createdContext;
    }
}
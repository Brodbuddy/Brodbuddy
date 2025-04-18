using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Postgres;

namespace Infrastructure.Data.Tests;

public static class TestDataSeeder
{
    public static async Task<User> SeedUserAsync(this PostgresDbContext context,
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

    public static async Task<Device> SeedDeviceAsync(this PostgresDbContext context, TimeProvider timeProvider,
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

    public static async Task<OneTimePassword> SeedOtpAsync(this PostgresDbContext context, TimeProvider timeProvider,
        int expiresMinutes = 15, int code = 123456)
    {
        var now = timeProvider.Now();
        var otp = new OneTimePassword
        {
            Code = code,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(expiresMinutes)
        };
        await context.OneTimePasswords.AddAsync(otp);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return otp;
    }
}
using Core.Entities;
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
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return user;
    }
}
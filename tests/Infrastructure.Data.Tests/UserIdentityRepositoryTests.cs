using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;
using Shouldly;
using Testcontainers.PostgreSql;

namespace Infrastructure.Data.Tests;

public class UserIdentityRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private PostgresDbContext _dbContext;
    private FakeTimeProvider _timeProvider;
    private PostgresUserIdentityRepository _repository;

    public UserIdentityRepositoryTests()
    {
        // Opsætning af PostgreSQL container til testning.
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithImage("postgres:16")
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        _dbContext = new PostgresDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresUserIdentityRepository(_dbContext, _timeProvider);
    }
    
    [Fact]
    public async Task SaveAsync_WithValidEmail_SavesUserToDatabase()
    {
        // Arrange
        var email = "Test@email.com";
        
        // Act
        Guid userId = await _repository.SaveAsync(email);

        //Assert
        var savedUser = await _dbContext.Users.FindAsync(userId);
        savedUser.ShouldNotBeNull();
        savedUser.Email.ShouldBe(email);
        savedUser.RegisterDate.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.StopAsync();
    }
}
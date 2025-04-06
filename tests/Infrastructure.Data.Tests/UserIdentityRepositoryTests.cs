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
    
    [Fact]
    public async Task ExistsAsync_WithExistingUserId_ReturnsTrue()
    {
        // Arrange
        var email = "test@email.com";
        var userId = await _repository.SaveAsync(email);

        // Act
        var exists = await _repository.ExistsAsync(userId);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingUserId_ReturnsFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistingId);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        var email = "test@email.com";
        await _repository.SaveAsync(email);

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingEmail_ReturnsFalse()
    {
        // Arrange
        var nonExistingEmail = "nonexisting@email.com";

        // Act
        var exists = await _repository.ExistsAsync(nonExistingEmail);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAsync_WithExistingUserId_ReturnsUser()
    {
        // Arrange
        var email = "test@email.com";
        var userId = await _repository.SaveAsync(email);

        // Act
        var user = await _repository.GetAsync(userId);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(userId);
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingUserId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(() => 
            _repository.GetAsync(nonExistingId));
    
        exception.Message.ShouldContain($"User with ID {nonExistingId} not found");
    }

    [Fact]
    public async Task GetAsync_WithExistingEmail_ReturnsUser()
    {
        // Arrange
        var email = "test@email.com";
        var userId = await _repository.SaveAsync(email);

        // Act
        var user = await _repository.GetAsync(email);

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(userId);
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingEmail_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExistingEmail = "nonexisting@email.com";

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(() => 
            _repository.GetAsync(nonExistingEmail));
    
        exception.Message.ShouldContain($"User with email {nonExistingEmail} not found");
    }

    [Fact]
    public async Task GetAsync_WithDifferentCaseEmail_ReturnsCorrectUser()
    {
        // Arrange
        var email = "Test@Email.com";
        var userId = await _repository.SaveAsync(email);

        // Act
        var user = await _repository.GetAsync("test@email.com");

        // Assert
        user.ShouldNotBeNull();
        user.Id.ShouldBe(userId);
        user.Email.ShouldBe(email);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.StopAsync();
    }
}
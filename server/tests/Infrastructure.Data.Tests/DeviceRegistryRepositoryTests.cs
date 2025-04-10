using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SharedTestDependencies;

namespace Infrastructure.Data.Tests;

public class DeviceRegistryRepositoryTests
{
    private readonly PostgresDbContext _dbContext;
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresDeviceRegistryRepository _repository;

    public DeviceRegistryRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PostgresDbContext(options);
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresDeviceRegistryRepository(_dbContext, _timeProvider);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateDeviceRegistry_AndReturnId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        // Act
        var registryId = await _repository.SaveAsync(userId, deviceId);

        // Assert
        registryId.ShouldNotBe(Guid.Empty);

        var savedRegistry = await _dbContext.DeviceRegistries.FindAsync(registryId);
        savedRegistry.ShouldNotBeNull();
        savedRegistry.UserId.ShouldBe(userId);
        savedRegistry.DeviceId.ShouldBe(deviceId);
        savedRegistry.CreatedAt.ShouldBe(_timeProvider.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task SaveAsync_WithValidInput_ShouldSaveChangesToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        // Act
        await _repository.SaveAsync(userId, deviceId);

        // Assert
        var registry = await _dbContext.DeviceRegistries
            .FirstOrDefaultAsync(r => r.UserId == userId && r.DeviceId == deviceId);

        registry.ShouldNotBeNull();
        registry.UserId.ShouldBe(userId);
        registry.DeviceId.ShouldBe(deviceId);
    }
}
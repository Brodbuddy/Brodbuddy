using Core.Extensions;
using Infrastructure.Data.Postgres;
using Shouldly;
using SharedTestDependencies;

namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class DeviceRegistryRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresDeviceRegistryRepository _repository;

    public DeviceRegistryRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresDeviceRegistryRepository(_dbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : DeviceRegistryRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_ShouldCreateDeviceRegistry_AndReturnId()
        {
            // Arrange
            var user = await _dbContext.SeedUserAsync(_timeProvider);
            var device = await _dbContext.SeedDeviceAsync(_timeProvider);
            var expectedTime = _timeProvider.Now();

            // Act
            var registryId = await _repository.SaveAsync(user.Id, device.Id);

            // Assert
            registryId.ShouldNotBe(Guid.Empty);

            var savedRegistry = await _dbContext.DeviceRegistries.FindAsync(registryId);
            savedRegistry.ShouldNotBeNull();
            savedRegistry.UserId.ShouldBe(user.Id);
            savedRegistry.DeviceId.ShouldBe(device.Id);
            savedRegistry.CreatedAt.ShouldBeWithinTolerance(expectedTime);
        }
    }
}
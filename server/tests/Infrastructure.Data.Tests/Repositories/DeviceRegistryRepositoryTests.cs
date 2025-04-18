using Core.Extensions;
using Infrastructure.Data.Postgres;
using Infrastructure.Data.Tests.Bases;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Database;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class DeviceRegistryRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PostgresDeviceRegistryRepository _repository;

    private DeviceRegistryRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresDeviceRegistryRepository(DbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : DeviceRegistryRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_ShouldCreateDeviceRegistry_AndReturnId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var expectedTime = _timeProvider.Now();

            // Act
            var registryId = await _repository.SaveAsync(user.Id, device.Id);

            // Assert
            registryId.ShouldNotBe(Guid.Empty);

            var savedRegistry = await DbContext.DeviceRegistries.FindAsync(registryId);
            savedRegistry.ShouldNotBeNull();
            savedRegistry.UserId.ShouldBe(user.Id);
            savedRegistry.DeviceId.ShouldBe(device.Id);
            savedRegistry.CreatedAt.ShouldBeWithinTolerance(expectedTime);
        }
    }
}
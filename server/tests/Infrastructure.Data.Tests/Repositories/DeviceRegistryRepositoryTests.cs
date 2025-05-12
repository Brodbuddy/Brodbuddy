using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Tests.Bases;
using Infrastructure.Data.Tests.Database;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies.Constants;
using SharedTestDependencies.Extensions;
using SharedTestDependencies.Fakes;
using SharedTestDependencies.Fixtures;
using Shouldly;

namespace Infrastructure.Data.Tests.Repositories;

[Collection(TestCollections.Database)]
public class DeviceRegistryRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgDeviceRegistryRepository _repository;

    private DeviceRegistryRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgDeviceRegistryRepository(DbContext, _timeProvider);
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

            var savedRegistry = await DbContext.DeviceRegistries.AsNoTracking().FirstOrDefaultAsync(dr => dr.Id == registryId);
            savedRegistry.ShouldNotBeNull();
            savedRegistry.UserId.ShouldBe(user.Id);
            savedRegistry.DeviceId.ShouldBe(device.Id);
            savedRegistry.CreatedAt.ShouldBeWithinTolerance(expectedTime);
        }
    }
}
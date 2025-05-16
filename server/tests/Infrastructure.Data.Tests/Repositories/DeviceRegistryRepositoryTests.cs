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
            var fingerprint = "device-fingerprint-hash-123";

            // Act
            var registryId = await _repository.SaveAsync(user.Id, device.Id, fingerprint);

            // Assert
            registryId.ShouldNotBe(Guid.Empty);

            var savedRegistry = await DbContext.DeviceRegistries.AsNoTracking().FirstOrDefaultAsync(dr => dr.Id == registryId);
            savedRegistry.ShouldNotBeNull();
            savedRegistry.UserId.ShouldBe(user.Id);
            savedRegistry.DeviceId.ShouldBe(device.Id);
            savedRegistry.Fingerprint.ShouldBe(fingerprint);
            savedRegistry.CreatedAt.ShouldBeWithinTolerance(expectedTime);
        }
    }
    
    public class GetDeviceIdByFingerprintAsync(PostgresFixture fixture) : DeviceRegistryRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetDeviceIdByFingerprintAsync_WithExistingFingerprint_ReturnsDeviceId()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var fingerprint = "device-fingerprint-hash-456";
            
            await _repository.SaveAsync(user.Id, device.Id, fingerprint);
            
            // Act
            var result = await _repository.GetDeviceIdByFingerprintAsync(user.Id, fingerprint);
            
            // Assert
            result.ShouldNotBeNull();
            result.ShouldBe(device.Id);
        }
        
        [Fact]
        public async Task GetDeviceIdByFingerprintAsync_WithNonExistentFingerprint_ReturnsNull()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var nonExistentFingerprint = "non-existent-fingerprint";
            
            // Act
            var result = await _repository.GetDeviceIdByFingerprintAsync(user.Id, nonExistentFingerprint);
            
            // Assert
            result.ShouldBeNull();
        }
        
        [Fact]
        public async Task GetDeviceIdByFingerprintAsync_WithExistingFingerprintButDifferentUser_ReturnsNull()
        {
            // Arrange
            var user1 = await DbContext.SeedUserAsync(_timeProvider);
            var user2 = await DbContext.SeedUserAsync(_timeProvider);
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var fingerprint = "device-fingerprint-hash-789";
            
            await _repository.SaveAsync(user1.Id, device.Id, fingerprint);
            
            // Act 
            var result = await _repository.GetDeviceIdByFingerprintAsync(user2.Id, fingerprint);
            
            // Assert
            result.ShouldBeNull();
        }
    }
    
    public class CountByUserIdAsync(PostgresFixture fixture) : DeviceRegistryRepositoryTests(fixture)
    {
        [Fact]
        public async Task CountByUserIdAsync_WithExistingDevices_ReturnsCorrectCount()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device1 = await DbContext.SeedDeviceAsync(_timeProvider);
            var device2 = await DbContext.SeedDeviceAsync(_timeProvider);
            var device3 = await DbContext.SeedDeviceAsync(_timeProvider);
            
            await _repository.SaveAsync(user.Id, device1.Id, "fingerprint-1");
            await _repository.SaveAsync(user.Id, device2.Id, "fingerprint-2");
            await _repository.SaveAsync(user.Id, device3.Id, "fingerprint-3");
            
            // Act
            var count = await _repository.CountByUserIdAsync(user.Id);
            
            // Assert
            count.ShouldBe(3);
        }
        
        [Fact]
        public async Task CountByUserIdAsync_WithNoDevices_ReturnsZero()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            
            // Act
            var count = await _repository.CountByUserIdAsync(user.Id);
            
            // Assert
            count.ShouldBe(0);
        }
        
        [Fact]
        public async Task CountByUserIdAsync_CountsUniqueDeviceIds()
        {
            // Arrange
            var user = await DbContext.SeedUserAsync(_timeProvider);
            var device1 = await DbContext.SeedDeviceAsync(_timeProvider);
            var device2 = await DbContext.SeedDeviceAsync(_timeProvider);
            
            await _repository.SaveAsync(user.Id, device1.Id, "fingerprint-1");
            await _repository.SaveAsync(user.Id, device2.Id, "fingerprint-2");
            
            // Act
            var count = await _repository.CountByUserIdAsync(user.Id);
            
            // Assert
            count.ShouldBe(2);
        }
    }
}
using Core.Entities;
using Core.Extensions;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Repositories.Auth;
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
public class DeviceRepositoryTests : RepositoryTestBase
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly PgDeviceRepository _repository;

    private DeviceRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PgDeviceRepository(DbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task SaveAsync_WithNewDevice_SetsPropertiesAndReturnsId()
        {
            // Arrange
            var now = _timeProvider.Now();
            var device = new Device
            {
                Id = Guid.Empty,
                Name = "chrome_windows",
                Browser = "chrome",
                Os = "windows",
            };

            // Act
            var id = await _repository.SaveAsync(device);

            // Assert
            id.ShouldNotBe(Guid.Empty);
            var savedDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

            savedDevice.ShouldNotBeNull();
            savedDevice.Id.ShouldBe(id); 
            savedDevice.Name.ShouldBe("chrome_windows");
            savedDevice.Browser.ShouldBe("chrome");
            savedDevice.Os.ShouldBe("windows");
            
            // Verificer properties saf af METODEN
            savedDevice.CreatedAt.ShouldBeWithinTolerance(now);
            savedDevice.LastSeenAt.ShouldBeWithinTolerance(now);
            savedDevice.IsActive.ShouldBeTrue();
        }

        [Fact]
        public async Task SaveAsync_WithNullDevice_ThrowsNullReferenceException()
        {
            // Arrange
            Device device = null!;

            // Act & Assert 
            await Should.ThrowAsync<ArgumentNullException>(() => _repository.SaveAsync(device));
        }
        
        [Fact]
        public async Task SaveAsync_WithExistingDeviceId_ThrowsArgumentException()
        {
            // Arrange
            var device = new Device
            {
                Id = Guid.NewGuid(), 
                Name = "firefox_macos",
                Browser = "firefox",
                Os = "macos"
            };

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _repository.SaveAsync(device));
        }
    }

    public class GetAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetAsync_WithExistingId_ReturnsDevice()
        {
            // Arrange
            var device = await DbContext.SeedDeviceAsync(_timeProvider, "macos", "chrome");
            
            // Act
            var result = await _repository.GetAsync(device.Id);

            // Assert
            result.ShouldNotBeNull();
            
            result.Id.ShouldBe(device.Id);
            result.Name.ShouldBe(device.Name);
            result.Browser.ShouldBe(device.Browser);
            result.Os.ShouldBe(device.Os);
            result.IsActive.ShouldBe(device.IsActive);
        }

        [Fact]
        public async Task GetAsync_WithNonExistingId_ThrowsArgumentException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _repository.GetAsync(nonExistingId));
        }
        
        [Fact]
        public async Task GetAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _repository.GetAsync(emptyId));
        }
    }

    public class GetByIdsAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task GetByIdsAsync_WithExistingIds_ReturnsMatchingDevices()
        {
            // Arrange
            var device1 = await DbContext.SeedDeviceAsync(_timeProvider, "windows", "chrome");
            var device2 = await DbContext.SeedDeviceAsync(_timeProvider, "macos", "chrome"); 
            var device3 = await DbContext.SeedDeviceAsync(_timeProvider, "ubuntu", "librewolf");
            var nonExistingId = Guid.NewGuid();
            
            var ids = new[] { device1.Id, device3.Id, nonExistingId };
            
            // Act
            var results = await _repository.GetByIdsAsync(ids);

            // Assert
            var result = results.ToList(); 
            
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2); 
            result.ShouldContain(d => d.Id == device1.Id);
            result.ShouldContain(d => d.Id == device3.Id);
            result.ShouldNotContain(d => d.Id == device2.Id);
            result.ShouldNotContain(d => d.Id == nonExistingId); 
            result.First(d => d.Id == device1.Id).Name.ShouldBe(device1.Name);
        }

        [Fact]
        public async Task GetByIdsAsync_WithEmptyIdsList_ReturnsEmptyCollection()
        {
            // Arrange
            await DbContext.SeedDeviceAsync(_timeProvider); // Ensure DB isn't empty
            var emptyIds = Array.Empty<Guid>();

            // Act
            var results = await _repository.GetByIdsAsync(emptyIds);

            // Assert
            results.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithNonExistingIds_ReturnsEmptyCollection()
        {
            // Arrange
            var nonExistingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
            await DbContext.SeedDeviceAsync(_timeProvider);

            // Act
            var results = await _repository.GetByIdsAsync(nonExistingIds);

            // Assert
            results.ShouldBeEmpty();
        }
        
        [Fact]
        public async Task GetByIdsAsync_WithNullIds_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<Guid> nullIds = null!; 

            // Act & Assert
            await Should.ThrowAsync<ArgumentNullException>(() => _repository.GetByIdsAsync(nullIds));
        }
    }

    public class ExistsAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var device = await DbContext.SeedDeviceAsync(_timeProvider);

            // Act
            var exists = await _repository.ExistsAsync(device.Id);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            await DbContext.SeedDeviceAsync(_timeProvider);

            // Act
            var exists = await _repository.ExistsAsync(nonExistingId);

            // Assert
            exists.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ExistsAsync_WithEmptyId_ReturnsFalse()
        {
            // Arrange
            var emptyId = Guid.Empty;
            await DbContext.SeedDeviceAsync(_timeProvider);

            // Act
            var exists = await _repository.ExistsAsync(emptyId);

            // Assert
            exists.ShouldBeFalse();
        }
    }


    public class UpdateLastSeenAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task UpdateLastSeenAsync_WithExistingId_UpdatesTimeAndReturnsTrue()
        {
            // Arrange 
            var device = await DbContext.SeedDeviceAsync(_timeProvider);
            var newLastSeen = _timeProvider.Tomorrow();

            // Act
            await _repository.UpdateLastSeenAsync(device.Id, newLastSeen);

            // Assert
            var updatedDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.Id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.LastSeenAt.ShouldBeWithinTolerance(newLastSeen);
        }

        [Fact]
        public async Task UpdateLastSeenAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            var now = _timeProvider.Now();
            
            // Pre-assert
            (await DbContext.Devices.FindAsync(nonExistingId)).ShouldBeNull();
            
            // Act
            var result = await _repository.UpdateLastSeenAsync(nonExistingId, now);
            
            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task UpdateLastSeenAsync_WithMultipleDevices_OnlyUpdatesTargetDevice()
        {
            // Arrange
            var initialTargetTime = _timeProvider.Now().AddHours(-2);
            var initialOtherTime = _timeProvider.Now().AddHours(-1);
            
            var targetDevice = await DbContext.SeedDeviceAsync(_timeProvider, "windows", "chrome", lastSeenAt: initialTargetTime);
            var otherDevice = await DbContext.SeedDeviceAsync(_timeProvider, "macos", "safari", lastSeenAt: initialOtherTime);
            
            var newLastSeenTime = _timeProvider.Now();
            
            // Act
            var result = await _repository.UpdateLastSeenAsync(targetDevice.Id, newLastSeenTime);
            
            // Assert
            result.ShouldBeTrue();

            // Tjek at target device ER opdateret
            var reloadedTargetDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == targetDevice.Id);
            reloadedTargetDevice.ShouldNotBeNull();
            reloadedTargetDevice.LastSeenAt.ShouldBeWithinTolerance(newLastSeenTime);
            
            // Tjek at det andet device IKKE er opdateret
            var reloadedOtherDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == otherDevice.Id);
            reloadedOtherDevice.ShouldNotBeNull();
            reloadedOtherDevice.LastSeenAt.ShouldBeWithinTolerance(initialOtherTime);
        }
    }

    public class DisableAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
    {
        [Fact]
        public async Task DisableAsync_WithExistingActiveId_ReturnsTrueAndDisablesDevice()
        {
            // Arrange 
            var device = await DbContext.SeedDeviceAsync(_timeProvider, isActive: true);

            // Act
            var result = await _repository.DisableAsync(device.Id);

            // Assert
            result.ShouldBeTrue();

            var updatedDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.Id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.IsActive.ShouldBeFalse();
        }
        
        [Fact]
        public async Task DisableAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Pre-assert
            (await DbContext.Devices.FindAsync(nonExistingId)).ShouldBeNull();

            // Act
            var result = await _repository.DisableAsync(nonExistingId); 

            // Assert
            result.ShouldBeFalse(); 
        }
        
        [Fact]
        public async Task DisableAsync_WhenDeviceIsAlreadyInactive_ReturnsTrueAndDeviceRemainsInactive()
        {
            // Arrange
            var device = await DbContext.SeedDeviceAsync(_timeProvider, isActive: false); 

            // Act
            var result = await _repository.DisableAsync(device.Id); 

            // Assert
            result.ShouldBeTrue(); 
            
            var updatedDevice = await DbContext.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == device.Id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.IsActive.ShouldBeFalse(); 
        }
    }
}
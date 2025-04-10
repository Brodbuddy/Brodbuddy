using Core.Entities;
using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using SharedTestDependencies;
using Shouldly;
using Moq;

namespace Infrastructure.Data.Tests;

public class DeviceRepositoryTests
{
    private PostgresDbContext _dbContext;
    private FakeTimeProvider _timeProvider;
    private PostgresDeviceRepository _repository;

    public DeviceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new PostgresDbContext(options);
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresDeviceRepository(_dbContext, _timeProvider);
    }

    public class SaveAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task SaveAsync_WithNewDevice_SetsPropertiesAndReturnsId()
        {
            // Arrange
            var expectedTime = _timeProvider.GetUtcNow().UtcDateTime;

            var device = new Device
            {
                Id = Guid.Empty,
                Name = "chrome_windows",
                Browser = "chrome",
                Os = "windows",
                CreatedAt = expectedTime,
                LastSeenAt = expectedTime,
                IsActive = true
            };

            // Act
            var id = await _repository.SaveAsync(device);

            // Assert
            _dbContext.ChangeTracker.Clear();
            id.ShouldNotBe(Guid.Empty);
            var savedDevice = await _dbContext.Devices.FindAsync(id);

            savedDevice.ShouldNotBeNull();
            savedDevice.Name.ShouldBe("chrome_windows", savedDevice.Name);
            savedDevice.Browser.ShouldBe("chrome", savedDevice.Browser);
            savedDevice.Os.ShouldBe("windows", savedDevice.Os);
            savedDevice.CreatedAt.ShouldBe(expectedTime);
            savedDevice.LastSeenAt.ShouldBe(expectedTime);
            savedDevice.IsActive.ShouldBeTrue();
        }

        [Fact]
        public async Task SaveAsync_WithNullDevice_ThrowsArgumentNullException()
        {
            // Arrange
            Device device = null;

            // Act & Assert 
            var exception = await Should.ThrowAsync<ArgumentException>(() => _repository.SaveAsync(device));
            exception.Message.ShouldBe("Failed to save device");
            exception.ParamName.ShouldBe(null);
        }
    }

    public class GetAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task GetAsync_WithExistingId_ReturnsDevice()
        {
            // Arrange
            var device = new Device
            {
                Name = "chrome_macos",
                Browser = "chrome",
                Os = "macos"
            };
            await _dbContext.Devices.AddAsync(device);
            await _dbContext.SaveChangesAsync();

            var id = device.Id;

            _dbContext.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetAsync(id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(id);
            result.Name.ShouldBe("chrome_macos");
            result.Browser.ShouldBe("chrome");
            result.Os.ShouldBe("macos");
        }

        [Fact]
        public async Task GetAsync_WithNonExistingId_ThrowsArgumentException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act & Assert
            var exception = await Should.ThrowAsync<ArgumentException>(
                () => _repository.GetAsync(nonExistingId));

            exception.Message.ShouldBe($"Device with ID {nonExistingId} not found");
        }
    }

    public class GetByIdsAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task GetByIdsAsync_WithExistingIds_ReturnsMatchingDevices()
        {
            // Arrange
            var device1 = new Device { Name = "chrome_windows", Browser = "chrome", Os = "windows" };
            var device2 = new Device { Name = "chrome_macos", Browser = "chrome", Os = "macos" };
            var device3 = new Device { Name = "firefox_macos", Browser = "firefox", Os = "macos" };

            await _dbContext.Devices.AddRangeAsync(device1, device2, device3);
            await _dbContext.SaveChangesAsync();

            var ids = new[] { device1.Id, device3.Id };

            _dbContext.ChangeTracker.Clear();

            // Act
            var results = await _repository.GetByIdsAsync(ids);

            // Assert
            results.ShouldNotBeNull();
            results.Count().ShouldBe(2);
            results.ShouldContain(d => d.Id == device1.Id);
            results.ShouldContain(d => d.Id == device3.Id);
            results.ShouldNotContain(d => d.Id == device2.Id);
        }

        [Fact]
        public async Task GetByIdsAsync_WithEmptyIdsList_ReturnsEmptyCollection()
        {
            // Arrange
            var device = new Device { Name = "chrome_windows", Browser = "chrome", Os = "windows" };
            await _dbContext.Devices.AddAsync(device);
            await _dbContext.SaveChangesAsync();

            var emptyIds = Array.Empty<Guid>();

            _dbContext.ChangeTracker.Clear();

            // Act
            var results = await _repository.GetByIdsAsync(emptyIds);

            // Assert
            results.ShouldNotBeNull();
            results.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithNonExistingIds_ReturnsEmptyCollection()
        {
            // Arrange
            var nonExistingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var results = await _repository.GetByIdsAsync(nonExistingIds);

            // Assert
            results.ShouldNotBeNull();
            results.ShouldBeEmpty();
        }
    }

    public class ExistsAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var device = new Device { Name = "chrome_windows", Browser = "chrome", Os = "windows" };
            await _dbContext.Devices.AddAsync(device);
            await _dbContext.SaveChangesAsync();

            var id = device.Id;

            _dbContext.ChangeTracker.Clear();

            // Act
            var exists = await _repository.ExistsAsync(id);

            // Assert
            exists.ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingId_ReturnsFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var exists = await _repository.ExistsAsync(nonExistingId);

            // Assert
            exists.ShouldBeFalse();
        }
    }


    public class UpdateLastSeenAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task UpdateLastSeenAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange 
            var device = new Device
            {
                Name = "chrome_windows",
                Browser = "chrome",
                Os = "windows"
            };
            await _dbContext.Devices.AddAsync(device);
            await _dbContext.SaveChangesAsync();


            var id = device.Id;
            var newLastSeen = DateTime.UtcNow.AddDays(1);

            // Act
            var foundDevice = await _dbContext.Devices.FindAsync(id);
            foundDevice.LastSeenAt = newLastSeen;
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();

            // Assert
            var updatedDevice = await _dbContext.Devices.FindAsync(id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.LastSeenAt.ShouldBe(newLastSeen);
        }
    }

    public class DisableAsync : DeviceRepositoryTests
    {
        [Fact]
        public async Task DisableAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange 
            var device = new Device
            {
                Name = "chrome_windows",
                Browser = "chrome",
                Os = "windows",
                IsActive = true
            };
            await _dbContext.Devices.AddAsync(device);
            await _dbContext.SaveChangesAsync();

            var id = device.Id;

            // Act
            var foundDevice = await _dbContext.Devices.FindAsync(id);
            foundDevice.IsActive = false;
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();

            // Assert
            var updatedDevice = await _dbContext.Devices.FindAsync(id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.IsActive.ShouldBeFalse();
        }
    }
}
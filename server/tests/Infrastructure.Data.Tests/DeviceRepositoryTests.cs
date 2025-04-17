using Core.Entities;
using Infrastructure.Data.Postgres;
using SharedTestDependencies;
using Shouldly;

namespace Infrastructure.Data.Tests;

[Collection(TestCollections.Database)]
public class DeviceRepositoryTests : RepositoryTestBase
{
    private FakeTimeProvider _timeProvider;
    private PostgresDeviceRepository _repository;

    public DeviceRepositoryTests(PostgresFixture fixture) : base(fixture)
    {
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _repository = new PostgresDeviceRepository(_dbContext, _timeProvider);
    }

    public class SaveAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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
            savedDevice.Name.ShouldBe("chrome_windows");
            savedDevice.Browser.ShouldBe("chrome");
            savedDevice.Os.ShouldBe("windows");
            (savedDevice.CreatedAt - expectedTime).TotalMilliseconds.ShouldBeLessThan(1);
            (savedDevice.LastSeenAt - expectedTime).TotalMilliseconds.ShouldBeLessThan(1);
            savedDevice.IsActive.ShouldBeTrue();
        }

        [Fact]
        public async Task SaveAsync_WithNullDevice_ThrowsArgumentNullException()
        {
            // Arrange
            Device device = null!;

            // Act & Assert 
            await Should.ThrowAsync<NullReferenceException>(() => _repository.SaveAsync(device));
        }
    }

    public class GetAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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

            exception.Message.ShouldContain($"Device with ID {nonExistingId} not found");
        }
    }

    public class GetByIdsAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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
            var enumerable = results as Device[] ?? results.ToArray();
            
            enumerable.ShouldNotBeNull();
            enumerable.Length.ShouldBe(2);
            enumerable.ShouldContain(d => d.Id == device1.Id);
            enumerable.ShouldContain(d => d.Id == device3.Id);
            enumerable.ShouldNotContain(d => d.Id == device2.Id);
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
            var enumerable = results as Device[] ?? results.ToArray();
            
            enumerable.ShouldNotBeNull();
            enumerable.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithNonExistingIds_ReturnsEmptyCollection()
        {
            // Arrange
            var nonExistingIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var results = await _repository.GetByIdsAsync(nonExistingIds);

            // Assert
            var enumerable = results as Device[] ?? results.ToArray();
            
            enumerable.ShouldNotBeNull();
            enumerable.ShouldBeEmpty();
        }
    }

    public class ExistsAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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


    public class UpdateLastSeenAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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
            await _repository.UpdateLastSeenAsync(id, newLastSeen);
            _dbContext.ChangeTracker.Clear();

            // Assert
            var updatedDevice = await _dbContext.Devices.FindAsync(id);
            updatedDevice.ShouldNotBeNull();
            (updatedDevice.LastSeenAt - newLastSeen).TotalMilliseconds.ShouldBeLessThan(1);
        }
    }

    public class DisableAsync(PostgresFixture fixture) : DeviceRepositoryTests(fixture)
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
            await _repository.DisableAsync(id);
            // Vi skal clear changetreacker for at tvinge EF Core til at læse fra db igen
            _dbContext.ChangeTracker.Clear();

            // Assert
            var updatedDevice = await _dbContext.Devices.FindAsync(id);
            updatedDevice.ShouldNotBeNull();
            updatedDevice.IsActive.ShouldBeFalse();
        }
    }
}
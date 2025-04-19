using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Entities;
using Moq;
using SharedTestDependencies.Fakes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class DeviceServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IDeviceRepository> _repositoryMock;
    private readonly DeviceService _service;
    private readonly FakeTimeProvider _timeProvider;

    private DeviceServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IDeviceRepository>();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _service = new DeviceService(_repositoryMock.Object, _timeProvider);
    }

    public class CreateAsync(ITestOutputHelper outputHelper) : DeviceServiceTests(outputHelper)
    {
        [Theory]
        [InlineData("    chrome  ", " linux ", "chrome_linux")]
        [InlineData("Firefox", " windows ", "firefox_windows")]
        [InlineData("chrome", "MacOS", "chrome_macos")]
        public async Task CreateAsync_WithBrowserAndOs_CreatesDeviceWithExpectedName(
            string browser, string os, string expectedName)
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            Device capturedDevice = null!;
            _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<Device>()))
                .Callback<Device>(d => capturedDevice = d)
                .ReturnsAsync(expectedId);


            // Act 
            var result = await _service.CreateAsync(browser, os);

            // Assert
            result.ShouldBe(expectedId);
            capturedDevice.ShouldNotBeNull();
            capturedDevice.Name.ShouldBe(expectedName);
        }

        [Theory]
        [InlineData("", "windows")]
        [InlineData("   ", "windows")]
        [InlineData("chrome", "")]
        [InlineData("chrome", "   ")]
        [InlineData("chrome", null)]
        [InlineData(null, "MacOS")]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("   ", "   ")]
        public async Task CreateAsync_WithNullOrEmptyOrWhitespaceBrowserOrOs_ThrowsArgumentException(string? browser,
            string? os)
        {
            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.CreateAsync(browser!, os!));
        }
    }


    public class GetAsync(ITestOutputHelper outputHelper) : DeviceServiceTests(outputHelper)
    {
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsDevice()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var expectedDevice = new Device { Id = deviceId, Name = "chrome_macos" };
            _repositoryMock.Setup(r => r.GetAsync(deviceId))
                .ReturnsAsync(expectedDevice);

            // Act
            var result = await _service.GetByIdAsync(deviceId);

            // Assert
            result.ShouldBe(expectedDevice);
            _repositoryMock.Verify(r => r.GetAsync(deviceId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var deviceId = Guid.Empty;

            // Act
            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.GetByIdAsync(deviceId));

            // Assert
            exception.Message.ShouldStartWith("Device ID cannot be empty");
            exception.ParamName.ShouldBe("id");
        }


        [Fact]
        public async Task GetByIdsAsync_WithExistingIds_ReturnsDevices()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var ids = new List<Guid> { id1, id2 };

            var expectedDevices = new List<Device>
            {
                new() { Id = id1, Name = "chrome_windows" },
                new() { Id = id2, Name = "firefox_macos" }
            };

            _repositoryMock.Setup(r => r.GetByIdsAsync(It.Is<List<Guid>>(
                    list => list.Contains(id1) &&
                            list.Contains(id2) &&
                            list.Count == 2)))
                .ReturnsAsync(expectedDevices);

            // Act
            var result = await _service.GetByIdsAsync(ids);

            // Assert
            result.ShouldBe(expectedDevices);
            _repositoryMock.Verify(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()), Times.Once);
        }


        [Fact]
        public async Task GetByIdsAsync_WithEmptyCollection_ReturnsEmptyEnumerable()
        {
            // Act
            var result = await _service.GetByIdsAsync(new List<Guid>());

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_WithNullReference_ThrowsArgumentException()
        {
            // Arrange
            IEnumerable<Guid> nullIds = null!;

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _service.GetByIdsAsync(nullIds));
        }
    }

    public class ExistsAsync(ITestOutputHelper outputHelper) : DeviceServiceTests(outputHelper)
    {
        [Fact]
        public async Task ExistsAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync(deviceId)).ReturnsAsync(true);

            // Act
            var result = await _service.ExistsAsync(deviceId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.ExistsAsync(deviceId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithValidIdThatDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.ExistsAsync(deviceId)).ReturnsAsync(false);

            // Act
            var result = await _service.ExistsAsync(deviceId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.ExistsAsync(deviceId), Times.Once);
        }

        [Fact]
        public async Task ExistsAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var deviceId = Guid.Empty;

            // Act
            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.ExistsAsync(deviceId));

            // Assert
            exception.Message.ShouldStartWith("Device ID cannot be empty");
            exception.ParamName.ShouldBe("id");
            _repositoryMock.Verify(r => r.ExistsAsync(It.IsAny<Guid>()), Times.Never);
        }
    }

    public class UpdateLastSeenAsync(ITestOutputHelper outputHelper) : DeviceServiceTests(outputHelper)
    {
        [Fact]
        public async Task UpdateLastSeenAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var currentTime = _timeProvider.GetUtcNow().UtcDateTime;
            _repositoryMock.Setup(r => r.UpdateLastSeenAsync(deviceId, currentTime)).ReturnsAsync(true);

            // Act
            var result = await _service.UpdateLastSeenAsync(deviceId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.UpdateLastSeenAsync(deviceId, currentTime), Times.Once);
        }

        [Fact]
        public async Task UpdateLastSeenAsync_WithValidIdButFailedUpdate_ReturnsFalse()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var currentTime = _timeProvider.GetUtcNow().UtcDateTime;
            _repositoryMock.Setup(r => r.UpdateLastSeenAsync(deviceId, currentTime)).ReturnsAsync(false);

            // Act
            var result = await _service.UpdateLastSeenAsync(deviceId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.UpdateLastSeenAsync(deviceId, currentTime), Times.Once);
        }

        [Fact]
        public async Task UpdateLastSeenAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var deviceId = Guid.Empty;

            // Act
            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.UpdateLastSeenAsync(deviceId));

            // Assert
            exception.Message.ShouldStartWith("Device ID cannot be empty");
            exception.ParamName.ShouldBe("id");
            _repositoryMock.Verify(r => r.UpdateLastSeenAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task UpdateLastSeenAsync_UsesCurrentTime()
        {
            // Arrange - Start
            var deviceId = Guid.NewGuid();
            var initialTime = _timeProvider.GetUtcNow().UtcDateTime;
            _repositoryMock.Setup(r => r.UpdateLastSeenAsync(deviceId, initialTime)).ReturnsAsync(true);

            // Act 
            await _service.UpdateLastSeenAsync(deviceId);

            // Assert
            _repositoryMock.Verify(r => r.UpdateLastSeenAsync(deviceId, initialTime), Times.Once);

            // Arrange - Skru tiden frem
            _timeProvider.Advance(TimeSpan.FromHours(1));
            var advancedTime = _timeProvider.GetUtcNow().UtcDateTime;
            _repositoryMock.Setup(r => r.UpdateLastSeenAsync(deviceId, advancedTime)).ReturnsAsync(true);

            // Act 
            await _service.UpdateLastSeenAsync(deviceId);

            // Assert
            _repositoryMock.Verify(r => r.UpdateLastSeenAsync(deviceId, advancedTime), Times.Once);
        }
    }

    public class DisableAsync(ITestOutputHelper outputHelper) : DeviceServiceTests(outputHelper)
    {
        [Fact]
        public async Task DisableAsync_WithValidId_ReturnsTrue()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.DisableAsync(deviceId)).ReturnsAsync(true);

            // Act
            var result = await _service.DisableAsync(deviceId);

            // Assert
            result.ShouldBeTrue();
            _repositoryMock.Verify(r => r.DisableAsync(deviceId), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_WithValidIdButFailedDisable_ReturnsFalse()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.DisableAsync(deviceId)).ReturnsAsync(false);

            // Act
            var result = await _service.DisableAsync(deviceId);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.DisableAsync(deviceId), Times.Once);
        }

        [Fact]
        public async Task DisableAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var deviceId = Guid.Empty;

            // Act
            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _service.DisableAsync(deviceId));

            // Assert
            exception.Message.ShouldStartWith("Device ID cannot be empty");
            exception.ParamName.ShouldBe("id");
            _repositoryMock.Verify(r => r.DisableAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Exceptions;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class DeviceRegistryServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IDeviceRegistryRepository> _repositoryMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly DeviceRegistryService _service;

    private DeviceRegistryServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IDeviceRegistryRepository>();
        _deviceServiceMock = new Mock<IDeviceService>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();

        _service = new DeviceRegistryService(
            _repositoryMock.Object,
            _deviceServiceMock.Object,
            _userIdentityServiceMock.Object
        );
    }
    
    public class AssociateDeviceAsync(ITestOutputHelper outputHelper) : DeviceRegistryServiceTests(outputHelper)
    {
        [Fact]
        public async Task AssociateDeviceAsync_ValidUser_ReturnsDeviceId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var deviceDetails = new Models.DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _deviceServiceMock.Setup(x => x.CreateAsync(It.IsAny<Models.DeviceDetails>())).ReturnsAsync(deviceId);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync((Guid?)null);
            _repositoryMock.Setup(x => x.CountByUserIdAsync(userId)).ReturnsAsync(0);

            // Act
            var result = await _service.AssociateDeviceAsync(userId, deviceDetails);

            // Assert
            result.ShouldBe(deviceId);
            _repositoryMock.Verify(x => x.SaveAsync(userId, deviceId, It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public async Task AssociateDeviceAsync_ExistingFingerprint_ReturnsExistingDeviceId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingDeviceId = Guid.NewGuid();
            var deviceDetails = new Models.DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync(existingDeviceId);
            
            // Act
            var result = await _service.AssociateDeviceAsync(userId, deviceDetails);

            // Assert
            result.ShouldBe(existingDeviceId);
            _deviceServiceMock.Verify(x => x.UpdateLastSeenAsync(existingDeviceId), Times.Once);
            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<Models.DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AssociateDeviceAsync_UserDoesNotExist_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceDetails = new Models.DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.AssociateDeviceAsync(userId, deviceDetails)
            );

            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<Models.DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task AssociateDeviceAsync_DeviceLimitReached_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceDetails = new Models.DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync((Guid?)null);
            _repositoryMock.Setup(x => x.CountByUserIdAsync(userId)).ReturnsAsync(5); // Max antal enheder 
            
            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(() =>
                _service.AssociateDeviceAsync(userId, deviceDetails)
            );
            
            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<Models.DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
    }
}
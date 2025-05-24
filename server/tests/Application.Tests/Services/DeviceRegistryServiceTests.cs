using Application.Interfaces;
using Application.Interfaces.Data;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Models.DTOs;
using Application.Services;
using Application.Services.Auth;
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
    private readonly Mock<ITransactionManager> _transactionManagerMock;
    private readonly DeviceRegistryService _service;
    

    private DeviceRegistryServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _repositoryMock = new Mock<IDeviceRegistryRepository>();
        _deviceServiceMock = new Mock<IDeviceService>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _transactionManagerMock = new Mock<ITransactionManager>();

        _service = new DeviceRegistryService(
            _repositoryMock.Object,
            _deviceServiceMock.Object,
            _userIdentityServiceMock.Object,
            _transactionManagerMock.Object
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
            var deviceDetails = new DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _deviceServiceMock.Setup(x => x.CreateAsync(It.IsAny<DeviceDetails>())).ReturnsAsync(deviceId);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync((Guid?)null);
            _repositoryMock.Setup(x => x.CountByUserIdAsync(userId)).ReturnsAsync(0);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns((Func<Task<Guid>> func) => func());
            
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
            var deviceDetails = new DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync(existingDeviceId);
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns<Func<Task<Guid>>>(async operation => await operation());
            
            // Act
            var result = await _service.AssociateDeviceAsync(userId, deviceDetails);

            // Assert
            result.ShouldBe(existingDeviceId);
            _deviceServiceMock.Verify(x => x.UpdateLastSeenAsync(existingDeviceId), Times.Once);
            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }

        [Fact]
        public async Task AssociateDeviceAsync_UserDoesNotExist_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceDetails = new DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns((Func<Task<Guid>> func) => func());

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.AssociateDeviceAsync(userId, deviceDetails)
            );

            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task AssociateDeviceAsync_DeviceLimitReached_ThrowsBusinessRuleViolationException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceDetails = new DeviceDetails("chrome", "windows", "Mozilla Firefox", "127.0.0.1");
            
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _repositoryMock.Setup(x => x.GetDeviceIdByFingerprintAsync(userId, It.IsAny<string>())).ReturnsAsync((Guid?)null);
            _repositoryMock.Setup(x => x.CountByUserIdAsync(userId)).ReturnsAsync(5); // Max antal enheder 
            
            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns<Func<Task<Guid>>>(async operation => await operation());
            
            // Act & Assert
            await Should.ThrowAsync<BusinessRuleViolationException>(() =>
                _service.AssociateDeviceAsync(userId, deviceDetails)
            );
            
            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<DeviceDetails>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }
    }
}
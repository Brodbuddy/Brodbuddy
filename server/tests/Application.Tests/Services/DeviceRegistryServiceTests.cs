using Application.Interfaces;
using Application.Interfaces.Data.Repositories;
using Application.Services;
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
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
            _deviceServiceMock.Setup(x => x.CreateAsync("chrome", "windows")).ReturnsAsync(deviceId);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns((Func<Task<Guid>> func) => func());
            
            // Act
            var result = await _service.AssociateDeviceAsync(userId, "chrome", "windows");

            // Assert
            result.ShouldBe(deviceId);
            _repositoryMock.Verify(x => x.SaveAsync(userId, deviceId), Times.Once);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }

        [Fact]
        public async Task AssociateDeviceAsync_UserDoesNotExist_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()))
                .Returns((Func<Task<Guid>> func) => func());

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _service.AssociateDeviceAsync(userId, "chrome", "windows")
            );
            
            _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<Guid>>>()), Times.Once);
        }
    }
}
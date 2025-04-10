using Application.Interfaces;
using Application.Services;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests;

public class DeviceRegistryServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IDeviceRegistryRepository> _repositoryMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IDeviceService> _deviceServiceMock;
    private readonly DeviceRegistryService _service;
    
    public DeviceRegistryServiceTests(ITestOutputHelper testOutputHelper)
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


    [Fact]
    public async Task AssociateDevice_ValidUser_ReturnsDeviceId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(true);
        _deviceServiceMock.Setup(x => x.CreateAsync("chrome", "windows")).ReturnsAsync(deviceId);

        // Act
        var result = await _service.AssociateDeviceAsync(userId, "chrome", "windows");

        // Assert
        result.ShouldBe(deviceId);
        _repositoryMock.Verify(x => x.SaveAsync(userId, deviceId), Times.Once);
    }

    [Fact]
    public async Task AssociateDevice_UserDoesNotExist_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userIdentityServiceMock.Setup(x => x.ExistsAsync(userId)).ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(
            async () => await _service.AssociateDeviceAsync(userId, "chrome", "windows")
        );

        _deviceServiceMock.Verify(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }
}
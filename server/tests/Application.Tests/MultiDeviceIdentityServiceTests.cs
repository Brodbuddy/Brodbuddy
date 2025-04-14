using Application.Interfaces;
using Application.Services;
using Core.Entities;
using Moq;
using SharedTestDependencies;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests;

public class MultiDeviceIdentityServiceTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Mock<IMultiDeviceIdentityRepository> _repositoryMock;
    private readonly Mock<IDeviceRegistryService> _deviceRegistryServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly IMultiDeviceIdentityService _multiDeviceIdentityService;


    public MultiDeviceIdentityServiceTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _repositoryMock = new Mock<IMultiDeviceIdentityRepository>();
        _deviceRegistryServiceMock = new Mock<IDeviceRegistryService>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _multiDeviceIdentityService = new MultiDeviceIdentityService(
            _repositoryMock.Object,
            _deviceRegistryServiceMock.Object,
            _refreshTokenServiceMock.Object,
            _jwtServiceMock.Object,
            _userIdentityServiceMock.Object);
    }

    public class EstablishIdentityAsync(ITestOutputHelper outputHelper) : MultiDeviceIdentityServiceTests(outputHelper)
    {
        [Fact]
        public async Task EstablishIdentityAsync_WithValidInput_ReturnsExpectedTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var browser = "chrome";
            var os = "macos";
            var deviceId = Guid.NewGuid();
            var email = "test@email.com";
            var tokenId = Guid.NewGuid();
            var expectedAccessToken = "access";
            var expectedRefreshToken = "refresh";

            var userInfo = new User { Email = email };

            _deviceRegistryServiceMock
                .Setup(x => x.AssociateDeviceAsync(userId, browser, os))
                .ReturnsAsync(deviceId);

            _userIdentityServiceMock
                .Setup(x => x.GetAsync(userId))
                .ReturnsAsync(userInfo);

            _refreshTokenServiceMock
                .Setup(x => x.GenerateAsync())
                .ReturnsAsync(expectedRefreshToken);

            _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(expectedRefreshToken))
                .ReturnsAsync((true, tokenId));

            _repositoryMock
                .Setup(x => x.SaveIdentityAsync(userId, deviceId, tokenId))
                .Returns(Task.CompletedTask);

            _jwtServiceMock
                .Setup(x => x.Generate(userId.ToString(), email, "user"))
                .Returns(expectedAccessToken);

            // Act
            var result = await _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os);

            // Assert
            result.accessToken.ShouldBe(expectedAccessToken);
            result.refreshToken.ShouldBe(expectedRefreshToken);
        }
    }


}
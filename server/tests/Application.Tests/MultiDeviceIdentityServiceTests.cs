using Application.Interfaces;
using Application.Services;
using Core.Entities;
using Moq;
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
                .Returns(Task.FromResult(Guid.NewGuid()));

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


    public class RefreshIdentityAsync(ITestOutputHelper outputHelper) : MultiDeviceIdentityServiceTests(outputHelper)
    {
        [Fact]
        public async Task RefreshIdentityAsync_WithValidInput_ReturnsExpectedTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            var email = "test@email.com";
            var oldTokenId = Guid.NewGuid();
            var newTokenId = Guid.NewGuid();
            var oldRefreshToken = "old-refresh";
            var newRefreshToken = "new-refresh";
            var expectedAccessToken = "access";

            var userInfo = new User { Email = email };
            var tokenContext = new TokenContext
            {
                UserId = userId,
                DeviceId = deviceId,
                User = userInfo
            };

            _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(oldRefreshToken))
                .ReturnsAsync((true, oldTokenId));

            _repositoryMock
                .Setup(x => x.GetAsync(oldTokenId))
                .ReturnsAsync(tokenContext);

            _refreshTokenServiceMock
                .Setup(x => x.RotateAsync(oldRefreshToken))
                .ReturnsAsync(newRefreshToken);

            _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(newRefreshToken))
                .ReturnsAsync((true, newTokenId));

            _repositoryMock
                .Setup(x => x.RevokeTokenContextAsync(oldTokenId))
                .Returns(Task.FromResult(true));

            _repositoryMock
                .Setup(x => x.SaveIdentityAsync(userId, deviceId, newTokenId))
                .Returns(Task.FromResult(Guid.NewGuid()));

            _jwtServiceMock
                .Setup(x => x.Generate(userId.ToString(), email, "user"))
                .Returns(expectedAccessToken);

            // Act
            var result = await _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken);

            // Assert
            result.accessToken.ShouldBe(expectedAccessToken);
            result.refreshToken.ShouldBe(newRefreshToken);
        }

        [Fact]
        public async Task RefreshIdentityAsync_WithNoTokenContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var oldTokenId = Guid.NewGuid();
            var oldRefreshToken = "old";
            TokenContext tokenContext = null;

            _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(oldRefreshToken))
                .ReturnsAsync((true, oldTokenId));

            _repositoryMock
                .Setup(x => x.GetAsync(oldTokenId))
                .ReturnsAsync(tokenContext);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                async () => await _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));

            exception.Message.ShouldBe("Failed to rotate refresh token");
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task RefreshIdentityAsync_IfNewRefreshTokenIsNullOrEmpty_ThrowsInvalidOperationException(
            string newRefreshToken)
        {
            // Arrange
            var oldTokenId = Guid.NewGuid();
            var oldRefreshToken = "old";
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();

            var tokenContext = new TokenContext
            {
                UserId = userId,
                DeviceId = deviceId,
                User = new User { Email = "test@email.com" }
            };

            _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(oldRefreshToken))
                .ReturnsAsync((true, oldTokenId));

            _repositoryMock
                .Setup(x => x.GetAsync(oldTokenId))
                .ReturnsAsync(tokenContext);

            _refreshTokenServiceMock
                .Setup(x => x.RotateAsync(oldRefreshToken))
                .ReturnsAsync(newRefreshToken);

            // Act & Assert
            var exception = await Should.ThrowAsync<InvalidOperationException>(
                async () => await _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));

            exception.Message.ShouldBe("Failed to rotate refresh token");
        }
    }


    [Fact]
    public async Task RefreshIdentityAsync_WithValidRepositoryCalls_ReturnsExpectedTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var email = "test@email.com";
        var oldTokenId = Guid.NewGuid();
        var newTokenId = Guid.NewGuid();
        var oldRefreshToken = "old-refresh";
        var newRefreshToken = "new-refresh";
        var expectedAccessToken = "access";

        var userInfo = new User { Email = email };
        var tokenContext = new TokenContext
        {
            UserId = userId,
            DeviceId = deviceId,
            User = userInfo
        };

        var revokeTokenCalled = false;
        Guid revokedTokenId = Guid.Empty;

        var saveIdentityCalled = false;
        Guid savedUserId = Guid.Empty;
        Guid savedDeviceId = Guid.Empty;
        Guid savedTokenId = Guid.Empty;

        _refreshTokenServiceMock
            .Setup(x => x.TryValidateAsync(oldRefreshToken))
            .ReturnsAsync((true, oldTokenId));

        _repositoryMock
            .Setup(x => x.GetAsync(oldTokenId))
            .ReturnsAsync(tokenContext);

        _refreshTokenServiceMock
            .Setup(x => x.RotateAsync(oldRefreshToken))
            .ReturnsAsync(newRefreshToken);

        _refreshTokenServiceMock
            .Setup(x => x.TryValidateAsync(newRefreshToken))
            .ReturnsAsync((true, newTokenId));

        _repositoryMock
            .Setup(x => x.RevokeTokenContextAsync(It.IsAny<Guid>()))
            .Callback<Guid>(tokenId =>
            {
                revokeTokenCalled = true;
                revokedTokenId = tokenId;
            })
            .Returns(Task.FromResult(true));

        _repositoryMock
            .Setup(x => x.SaveIdentityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()))
            .Callback<Guid, Guid, Guid>((u, d, t) =>
            {
                saveIdentityCalled = true;
                savedUserId = u;
                savedDeviceId = d;
                savedTokenId = t;
            })
            .Returns(Task.FromResult(Guid.NewGuid()));

        _jwtServiceMock
            .Setup(x => x.Generate(userId.ToString(), email, "user"))
            .Returns(expectedAccessToken);

        // Act
        var result = await _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken);

        // Assert
        result.accessToken.ShouldBe(expectedAccessToken);
        result.refreshToken.ShouldBe(newRefreshToken);

        revokeTokenCalled.ShouldBeTrue();
        revokedTokenId.ShouldBe(oldTokenId);
        saveIdentityCalled.ShouldBeTrue();
        savedUserId.ShouldBe(userId);
        savedDeviceId.ShouldBe(deviceId);
        savedTokenId.ShouldBe(newTokenId);
    }
}
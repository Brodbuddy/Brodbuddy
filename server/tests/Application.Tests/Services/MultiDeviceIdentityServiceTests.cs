using Application.Interfaces.Data.Repositories;
using Application.Services;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Application.Tests.Services;

public class MultiDeviceIdentityServiceTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly Mock<IMultiDeviceIdentityRepository> _repositoryMock;
    private readonly Mock<IDeviceRegistryService> _deviceRegistryServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly IMultiDeviceIdentityService _multiDeviceIdentityService;


    private MultiDeviceIdentityServiceTests(ITestOutputHelper outputHelper)
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
            var deviceId = Guid.NewGuid();
            const string browser = "chrome";
            const string os = "macos";

            var userId = Guid.NewGuid();
            const string email = "test@email.com";
            var userInfo = new User { Id = userId, Email = email };

            var refreshTokenId = Guid.NewGuid();
            const string expectedAccessToken = "access-token-123";
            const string expectedRefreshToken = "refresh-token-abc";

            _deviceRegistryServiceMock.Setup(x => x.AssociateDeviceAsync(userId, browser, os)).ReturnsAsync(deviceId);
            _userIdentityServiceMock.Setup(x => x.GetAsync(userId)).ReturnsAsync(userInfo);
            _refreshTokenServiceMock.Setup(x => x.GenerateAsync()).ReturnsAsync((expectedRefreshToken, refreshTokenId));
            _repositoryMock.Setup(x => x.SaveIdentityAsync(userId, deviceId, refreshTokenId)).ReturnsAsync(Guid.NewGuid());
            _jwtServiceMock.Setup(x => x.Generate(userId.ToString(), email, "user")).Returns(expectedAccessToken);

            // Act
            var result = await _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os);

            // Assert
            result.accessToken.ShouldBe(expectedAccessToken);
            result.refreshToken.ShouldBe(expectedRefreshToken);

            _deviceRegistryServiceMock.Verify(x => x.AssociateDeviceAsync(userId, browser, os), Times.Once);
            _userIdentityServiceMock.Verify(x => x.GetAsync(userId), Times.Once);
            _refreshTokenServiceMock.Verify(x => x.GenerateAsync(), Times.Once);
            _repositoryMock.Verify(x => x.SaveIdentityAsync(userId, deviceId, refreshTokenId), Times.Once);
            _jwtServiceMock.Verify(x => x.Generate(userId.ToString(), email, "user"), Times.Once);
        }

        [Fact]
        public async Task EstablishIdentityAsync_WithEmptyUserId_ThrowsArgumentException()
        {
            // Arrange
            var emptyUserId = Guid.Empty;
            const string browser = "chrome";
            const string os = "macos";

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _multiDeviceIdentityService.EstablishIdentityAsync(emptyUserId, browser, os));
        }

        [Theory]
        [InlineData("", "freebsd")]
        [InlineData("   ", "freebsd")]
        [InlineData(null, "freebsd")]
        [InlineData("zen", "")]
        [InlineData("zen", "   ")]
        [InlineData("zen", null)]
        public async Task EstablishIdentityAsync_WithInvalidBrowserOrOs_ThrowsArgumentException(string? browser, string? os)
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() => _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser!, os!));
        }

        [Fact]
        public async Task EstablishIdentityAsync_WhenUserGetFails_ThrowsArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();
            const string browser = "safari";
            const string os = "freebsd";

            _deviceRegistryServiceMock.Setup(x => x.AssociateDeviceAsync(userId, browser, os)).ReturnsAsync(deviceId);

            // Simulering af fejl
            _userIdentityServiceMock.Setup(x => x.GetAsync(userId)).ThrowsAsync(new ArgumentException("User lookup failed"));

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(() =>
                _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os));

            _refreshTokenServiceMock.Verify(x => x.GenerateAsync(), Times.Never);
            _repositoryMock.Verify(x => x.SaveIdentityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task EstablishIdentityAsync_WhenRepositorySaveFails_ThrowsDbUpdateException()
        {
            // Arrange 
            var userId = Guid.NewGuid();
            const string email = "user@domain.com";
            var userInfo = new User { Id = userId, Email = email };

            var deviceId = Guid.NewGuid();
            const string browser = "firefox";
            const string os = "windows vista";


            var refreshTokenId = Guid.NewGuid();
            const string expectedRefreshToken = "refresh-token-xyz";

            _deviceRegistryServiceMock.Setup(x => x.AssociateDeviceAsync(userId, browser, os)).ReturnsAsync(deviceId);
            _userIdentityServiceMock.Setup(x => x.GetAsync(userId)).ReturnsAsync(userInfo);
            _refreshTokenServiceMock.Setup(x => x.GenerateAsync()).ReturnsAsync((expectedRefreshToken, refreshTokenId));

            _repositoryMock
                .Setup(x => x.SaveIdentityAsync(userId, deviceId, refreshTokenId))
                .ThrowsAsync(new DbUpdateException("Simulated database save failure", new Exception())); // Simuler repository fejl

            // Act & Assert
            await Should.ThrowAsync<DbUpdateException>(() => _multiDeviceIdentityService.EstablishIdentityAsync(userId, browser, os));

            _deviceRegistryServiceMock.Verify(x => x.AssociateDeviceAsync(userId, browser, os), Times.Once);
            _userIdentityServiceMock.Verify(x => x.GetAsync(userId), Times.Once);
            _refreshTokenServiceMock.Verify(x => x.GenerateAsync(), Times.Once);
            _repositoryMock.Verify(x => x.SaveIdentityAsync(userId, deviceId, refreshTokenId), Times.Once); // Burde være kaldt 1 gang (fejl)
            _jwtServiceMock.Verify(x => x.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never); // Burde ikke kalde JWT!!
        }
    }


    public class RefreshIdentityAsync(ITestOutputHelper outputHelper) : MultiDeviceIdentityServiceTests(outputHelper)
    {
        [Fact]
        public async Task RefreshIdentityAsync_WithValidInput_RotatesTokensAndReturnsNewPair()
        {
            // Arrange
            var userId = Guid.NewGuid();
            const string email = "test@email.com";
            var userInfo = new User { Email = email };
            
            var deviceId = Guid.NewGuid();
            
            var oldTokenId = Guid.NewGuid();
            var newTokenId = Guid.NewGuid();
            const string oldRefreshToken = "old-refresh";
            const string newRefreshToken = "new-refresh";
            const string expectedAccessToken = "access";
            
            var tokenContext = new TokenContext
            {
                UserId = userId,
                DeviceId = deviceId,
                User = userInfo,
                RefreshTokenId = oldTokenId
            };
            
            // 1. Opsæt validering af GAMMEL token -> Success
             _refreshTokenServiceMock
                .Setup(x => x.TryValidateAsync(oldRefreshToken))
                .ReturnsAsync((true, oldTokenId))
                .Verifiable("Validation of old token must be attempted"); 

            // 2. Opsæt det at hente context -> Success
            _repositoryMock
                .Setup(x => x.GetAsync(oldTokenId))
                .ReturnsAsync(tokenContext)
                .Verifiable("Fetching token context must be attempted");

            // 3. Opsæt rotation -> Success, returner NY token
            _refreshTokenServiceMock
                .Setup(x => x.RotateAsync(oldRefreshToken))
                .ReturnsAsync((newRefreshToken, newTokenId))
                .Verifiable("Token rotation must be attempted");

            // 4. Opsæt tilbagekaldelse of GAMMEL token (i RefreshTokenService) -> Success
            _refreshTokenServiceMock
                .Setup(x => x.RevokeAsync(oldRefreshToken))
                .ReturnsAsync(true) 
                .Verifiable("Revocation in RefreshTokenService must be attempted");

            // 5. Opsæt tilbagekaldelse af GAMMEL token context (i Repository) -> Success
            _repositoryMock
                .Setup(x => x.RevokeTokenContextAsync(oldTokenId)) 
                .ReturnsAsync(true)
                .Verifiable("Revocation in Repository must be attempted");

            // 6. Ospæt det at gamme NY identity context -> Success
            _repositoryMock
                .Setup(x => x.SaveIdentityAsync(userId, deviceId, newTokenId))
                .ReturnsAsync(Guid.NewGuid())
                .Verifiable("Saving new identity context must be attempted");

            // 7. Opsæt generering af NY access token  -> Success
            _jwtServiceMock
                .Setup(x => x.Generate(userId.ToString(), email, "user"))
                .Returns(expectedAccessToken)
                .Verifiable("Generation of new JWT must be attempted");

            // Act
            var result = await _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken);

            // Assert
            result.accessToken.ShouldBe(expectedAccessToken);
            result.refreshToken.ShouldBe(newRefreshToken);
            
            _refreshTokenServiceMock.Verify(); 
            _repositoryMock.Verify();        
            _jwtServiceMock.Verify();        
        }

        [Fact]
        public async Task RefreshIdentityAsync_WithNoTokenContext_ThrowsInvalidOperationException()
        {
            // Arrange
            var oldTokenId = Guid.NewGuid();
            const string oldRefreshToken = "old";
            TokenContext? tokenContext = null;

            _refreshTokenServiceMock.Setup(x => x.TryValidateAsync(oldRefreshToken)).ReturnsAsync((true, oldTokenId));
            _repositoryMock.Setup(x => x.GetAsync(oldTokenId)).ReturnsAsync(tokenContext);

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task RefreshIdentityAsync_IfNewRefreshTokenIsNullOrEmpty_ThrowsInvalidOperationException(string? newRefreshToken)
        {
            // Arrange
            var oldTokenId = Guid.NewGuid();
            const string oldRefreshToken = "old";
            var userId = Guid.NewGuid();
            var deviceId = Guid.NewGuid();

            var tokenContext = new TokenContext
            {
                UserId = userId,
                DeviceId = deviceId,
                User = new User { Email = "test@email.com" }
            };

            _refreshTokenServiceMock.Setup(x => x.TryValidateAsync(oldRefreshToken)).ReturnsAsync((true, oldTokenId));
            _repositoryMock.Setup(x => x.GetAsync(oldTokenId)).ReturnsAsync(tokenContext);
            _refreshTokenServiceMock.Setup(x => x.RotateAsync(oldRefreshToken))!.ReturnsAsync((newRefreshToken, Guid.Empty));

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() =>  _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));
        }
        
        [Fact]
        public async Task RefreshIdentityAsync_WhenTokenIsInvalid_ThrowsInvalidOperationException()
        {
            // Arrange
            const string oldRefreshToken = "invalid-or-expired-token";

            _refreshTokenServiceMock.Setup(x => x.TryValidateAsync(oldRefreshToken))
                                    .ReturnsAsync((false, Guid.Empty))
                                    .Verifiable(); 

            // Act & Assert
            await Should.ThrowAsync<InvalidOperationException>(() => _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));
            
            _refreshTokenServiceMock.Verify(); 
            _repositoryMock.Verify(x => x.GetAsync(It.IsAny<Guid>()), Times.Never);
            _refreshTokenServiceMock.Verify(x => x.RotateAsync(It.IsAny<string>()), Times.Never);
        }
        
        [Fact]
        public async Task RefreshIdentityAsync_WhenRepoRevokeFails_ThrowsException()
        {
            // Arrange 
            var userId = Guid.NewGuid();
            const string email = "test@email.com";
            var userInfo = new User { Id = userId, Email = email };
            
            var deviceId = Guid.NewGuid();
            
            var oldTokenId = Guid.NewGuid();
            var newTokenId = Guid.NewGuid();
            const string oldRefreshToken = "old-refresh";
            const string newRefreshToken = "new-refresh";
            
            var tokenContext = new TokenContext { UserId = userId, DeviceId = deviceId, User = userInfo, RefreshTokenId = oldTokenId };
            
            _refreshTokenServiceMock.Setup(x => x.TryValidateAsync(oldRefreshToken)).ReturnsAsync((true, oldTokenId));
            _repositoryMock.Setup(x => x.GetAsync(oldTokenId)).ReturnsAsync(tokenContext);
            _refreshTokenServiceMock.Setup(x => x.RotateAsync(oldRefreshToken)).ReturnsAsync((newRefreshToken, newTokenId));
            _refreshTokenServiceMock.Setup(x => x.RevokeAsync(oldRefreshToken)).ReturnsAsync(true); 

            // Opsættelse af fejl
            var expectedInnerException = new Exception("Simulated DB constraint violation during revoke");
            _repositoryMock.Setup(x => x.RevokeTokenContextAsync(oldTokenId))
                           .ThrowsAsync(expectedInnerException); // Simuler fejl

            // Act & Assert
            var actualException = await Should.ThrowAsync<Exception>(() => _multiDeviceIdentityService.RefreshIdentityAsync(oldRefreshToken));
            actualException.ShouldBeSameAs(expectedInnerException);
            
            _refreshTokenServiceMock.Verify(x => x.TryValidateAsync(oldRefreshToken), Times.Once);
            _repositoryMock.Verify(x => x.GetAsync(oldTokenId), Times.Once);
            _refreshTokenServiceMock.Verify(x => x.RotateAsync(oldRefreshToken), Times.Once);
            _refreshTokenServiceMock.Verify(x => x.RevokeAsync(oldRefreshToken), Times.Once);
            _repositoryMock.Verify(x => x.RevokeTokenContextAsync(oldTokenId), Times.Once); // Tjek at vi prøvede at tilgå det fejlede kald

            // Tjek at følgende kald IKKE skete
            _repositoryMock.Verify(x => x.SaveIdentityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            _jwtServiceMock.Verify(x => x.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
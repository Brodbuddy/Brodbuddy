using Application.Interfaces;
using Application.Services;
using Moq;
using Xunit;
using Shouldly;

namespace Application.Tests;

public class PasswordlessAuthServiceTests
{
    private readonly Mock<IUserRoleService> _mockUserRoleService;


    private readonly Mock<IIdentityVerificationService> _identityVerificationServiceMock;
    private readonly Mock<IMultiDeviceIdentityService> _multiDeviceIdentityServiceMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ITransactionManager> _transactionManagerMock;
    private readonly PasswordlessAuthService _passwordlessAuthService;

    public PasswordlessAuthServiceTests()
    {
        _identityVerificationServiceMock = new Mock<IIdentityVerificationService>();
        _multiDeviceIdentityServiceMock = new Mock<IMultiDeviceIdentityService>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _transactionManagerMock = new Mock<ITransactionManager>();
        _mockUserRoleService = new Mock<IUserRoleService>();
        _passwordlessAuthService = new PasswordlessAuthService(
            _identityVerificationServiceMock.Object,
            _multiDeviceIdentityServiceMock.Object,
            _userIdentityServiceMock.Object,
            _mockUserRoleService.Object,
            _transactionManagerMock.Object);
    }


    public class InitiateLoginAsync : PasswordlessAuthServiceTests
    {
        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("another@example.com", true)]
        [InlineData("invalid@example.com", false)]
        public async Task InitiateLoginAsync_ShouldCallSendCodeAsync_AndReturnExpectedResult(string email,
            bool expectedResult)
        {
            // Arrange
            _identityVerificationServiceMock.Setup(s => s.SendCodeAsync(email))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _passwordlessAuthService.InitiateLoginAsync(email);

            // Assert
            result.ShouldBe(expectedResult);
            _identityVerificationServiceMock.Verify(s => s.SendCodeAsync(email), Times.Once);
        }
    }

    public class CompleteLoginAsync : PasswordlessAuthServiceTests
    {
        [Theory]
        [InlineData("test@example.com", 123456, "Chrome", "Windows")]
        [InlineData("another@example.com", 654321, "Firefox", "macOS")]
        [InlineData("mobile@example.com", 112233, "Safari", "iOS")]
        public async Task CompleteLoginAsync_WithValidCode_ShouldReturnTokens(
            string email, int code, string browser, string os)
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var expectedTokens = ("access_token", "refresh_token");

            _identityVerificationServiceMock.Setup(s => s.TryVerifyCodeAsync(email, code))
                .ReturnsAsync((true, userId));
            _multiDeviceIdentityServiceMock
                .Setup(s => s.EstablishIdentityAsync(userId, It.IsAny<Models.DeviceDetails>()))
                .ReturnsAsync(expectedTokens);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()))
                .Returns((Func<Task<(string, string)>> func) => func());

            // Act
            var result = await _passwordlessAuthService.CompleteLoginAsync(email, code,
                new Models.DeviceDetails(browser, os, "Mozilla/5.0", "127.0.0.1"));

            // Assert
            result.accessToken.ShouldBe(expectedTokens.Item1);
            result.refreshToken.ShouldBe(expectedTokens.Item2);
            _identityVerificationServiceMock.Verify(s => s.TryVerifyCodeAsync(email, code), Times.Once);
            _multiDeviceIdentityServiceMock.Verify(
                s => s.EstablishIdentityAsync(userId, It.IsAny<Models.DeviceDetails>()), Times.Once);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()),
                Times.Once);
        }

        [Fact]
        public async Task CompleteLoginAsync_WithInvalidCode_ShouldThrowArgumentException()
        {
            // Arrange
            string email = "test@example.com";
            int code = 123456;
            var deviceDetails = new Models.DeviceDetails("Chrome", "Windows", "Mozilla/5.0", "127.0.0.1");

            _identityVerificationServiceMock.Setup(s => s.TryVerifyCodeAsync(email, code))
                .ReturnsAsync((false, Guid.Empty));

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()))
                .Returns((Func<Task<(string, string)>> func) => func());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _passwordlessAuthService.CompleteLoginAsync(email, code, deviceDetails));

            exception.ShouldBeOfType<ArgumentException>();
            _identityVerificationServiceMock.Verify(s => s.TryVerifyCodeAsync(email, code), Times.Once);
            _multiDeviceIdentityServiceMock.Verify(
                s => s.EstablishIdentityAsync(It.IsAny<Guid>(), It.IsAny<Models.DeviceDetails>()), Times.Never);

            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()),
                Times.Once);
        }
    }

    public class RefreshTokenAsync : PasswordlessAuthServiceTests
    {
        [Theory]
        [InlineData("refresh_token_1", "new_access_token_1", "new_refresh_token_1")]
        [InlineData("refresh_token_2", "new_access_token_2", "new_refresh_token_2")]
        public async Task RefreshTokenAsync_ShouldCallRefreshIdentityAsync(
            string refreshToken, string expectedAccessToken, string expectedRefreshToken)
        {
            // Arrange
            var expectedTokens = (expectedAccessToken, expectedRefreshToken);

            _multiDeviceIdentityServiceMock.Setup(s => s.RefreshIdentityAsync(refreshToken))
                .ReturnsAsync(expectedTokens);

            _transactionManagerMock
                .Setup(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()))
                .Returns((Func<Task<(string, string)>> func) => func());

            // Act
            var result = await _passwordlessAuthService.RefreshTokenAsync(refreshToken);

            // Assert
            result.accessToken.ShouldBe(expectedTokens.Item1);
            result.refreshToken.ShouldBe(expectedTokens.Item2);
            _multiDeviceIdentityServiceMock.Verify(s => s.RefreshIdentityAsync(refreshToken), Times.Once);
            _transactionManagerMock.Verify(tm => tm.ExecuteInTransactionAsync(It.IsAny<Func<Task<(string, string)>>>()),
                Times.Once);
        }
    }
}
using Application.Services;
using Moq;
using Xunit;
using Shouldly;

namespace Application.Tests;

public class PasswordlessAuthServiceTests
{
    private readonly Mock<IIdentityVerificationService> _mockIdentityVerificationService;
    private readonly Mock<IMultiDeviceIdentityService> _mockMultiDeviceIdentityService;
    private readonly Mock<IUserIdentityService> _mockUserIdentityService;
    private readonly PasswordlessAuthService _service;

    public PasswordlessAuthServiceTests()
    {
        _mockIdentityVerificationService = new Mock<IIdentityVerificationService>();
        _mockMultiDeviceIdentityService = new Mock<IMultiDeviceIdentityService>();
        _mockUserIdentityService = new Mock<IUserIdentityService>();
        _service = new PasswordlessAuthService(
            _mockIdentityVerificationService.Object,
            _mockMultiDeviceIdentityService.Object,
            _mockUserIdentityService.Object);
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
            _mockIdentityVerificationService.Setup(s => s.SendCodeAsync(email))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.InitiateLoginAsync(email);

            // Assert
            result.ShouldBe(expectedResult);
            _mockIdentityVerificationService.Verify(s => s.SendCodeAsync(email), Times.Once);
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

            _mockIdentityVerificationService.Setup(s => s.TryVerifyCodeAsync(email, code))
                .ReturnsAsync((true, userId));
            _mockMultiDeviceIdentityService.Setup(s => s.EstablishIdentityAsync(userId, browser, os))
                .ReturnsAsync(expectedTokens);

            // Act
            var result = await _service.CompleteLoginAsync(email, code, browser, os);

            // Assert
            result.accessToken.ShouldBe(expectedTokens.Item1);
            result.refreshToken.ShouldBe(expectedTokens.Item2);
            _mockIdentityVerificationService.Verify(s => s.TryVerifyCodeAsync(email, code), Times.Once);
            _mockMultiDeviceIdentityService.Verify(s => s.EstablishIdentityAsync(userId, browser, os), Times.Once);
        }

        [Fact]
        public async Task CompleteLoginAsync_WithInvalidCode_ShouldThrowUnauthorizedException()
        {
            // Arrange
            string email = "test@example.com";
            int code = 123456;
            string browser = "Chrome";
            string os = "Windows";

            _mockIdentityVerificationService.Setup(s => s.TryVerifyCodeAsync(email, code))
                .ReturnsAsync((false, Guid.Empty));

            // Act & Assert
            var exception =
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.CompleteLoginAsync(email, code, browser, os));

            exception.ShouldBeOfType<UnauthorizedAccessException>();
            _mockIdentityVerificationService.Verify(s => s.TryVerifyCodeAsync(email, code), Times.Once);
            _mockMultiDeviceIdentityService.Verify(
                s => s.EstablishIdentityAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
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

            _mockMultiDeviceIdentityService.Setup(s => s.RefreshIdentityAsync(refreshToken))
                .ReturnsAsync(expectedTokens);

            // Act
            var result = await _service.RefreshTokenAsync(refreshToken);

            // Assert
            result.accessToken.ShouldBe(expectedTokens.Item1);
            result.refreshToken.ShouldBe(expectedTokens.Item2);
            _mockMultiDeviceIdentityService.Verify(s => s.RefreshIdentityAsync(refreshToken), Times.Once);
        }
    }
}
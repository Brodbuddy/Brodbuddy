using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedTestDependencies;
using Xunit;

namespace Application.Tests;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly AppOptions _options;

    public JwtServiceTests()
    {
        var jwtOptions = new JwtOptions
        {
            Secret = "dfKDL0Rq26AEQhdHBcQkOvMNjj9S8/thdKhTVzm3UDWXfJ0gePCuWyf48VK9/hk1ID4VHqZjXpYhinms1r+Khg==",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 15
        };
        _options = new AppOptions { Jwt = jwtOptions };

        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
        mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(_options);

        var fixedTime = new DateTimeOffset(2025, 4, 2, 10, 0, 0, TimeSpan.Zero);
        _timeProvider = new FakeTimeProvider(fixedTime);

        var mockLogger = new Mock<ILogger<JwtService>>();
        var logger = mockLogger.Object;
        var optionsMonitor = mockOptionsMonitor.Object;
        _jwtService = new JwtService(optionsMonitor, _timeProvider, logger);
    }

    public class Generate : JwtServiceTests
    {
        [Fact]
        public void Generate_ShouldProduceValidToken_WithCorrectClaims()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";

            // Act
            var token = _jwtService.Generate(subject, email, role);

            // Assert
            Assert.True(_jwtService.TryValidate(token, out var decodedClaims));
            Assert.NotNull(decodedClaims);
            Assert.Equal(_options.Jwt.Issuer, decodedClaims.Iss);
            Assert.Equal(_options.Jwt.Audience, decodedClaims.Aud);
            Assert.Equal(subject, decodedClaims.Sub);
            Assert.Equal(email, decodedClaims.Email);
            Assert.Equal(role, decodedClaims.Role);
            Assert.True(decodedClaims.Exp > decodedClaims.Iat);
        }
    }

    public class TryValidate : JwtServiceTests
    {
        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenTokenFormatIsInvalid()
        {
            // Arrange
            var invalidToken = "this.is.not.a.valid.jwt.format";

            // Act
            var isValid = _jwtService.TryValidate(invalidToken, out var claims);

            // Assert
            Assert.False(isValid);
            Assert.Null(claims);
        }

        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenTokenIsExpired()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";
            var token = _jwtService.Generate(subject, email, role);

            // Act
            _timeProvider.Advance(TimeSpan.FromMinutes(16));
            var isValid = _jwtService.TryValidate(token, out var claims);

            // Assert
            Assert.False(isValid);
            Assert.Null(claims);
        }

        [Fact]
        public void TryValidate_ShouldUseTimeProvider_ToDetermineExpiration()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";
            var token = _jwtService.Generate(subject, email, role);

            // Act 1: Valider LIGE FØR udløb
            _timeProvider.Advance(TimeSpan.FromMinutes(14).Add(TimeSpan.FromSeconds(59)));
            var isValidBeforeExpiration = _jwtService.TryValidate(token, out var claimsBefore);

            // Act 2: Valider LIGE EFTER udløb
            _timeProvider.Advance(TimeSpan.FromSeconds(2)); // Nu 15 min + 1 sekund
            var isValidAfterExpiration = _jwtService.TryValidate(token, out var claimsAfter);

            // Assert
            Assert.True(isValidBeforeExpiration, "Token should be valid just before expiration");
            Assert.NotNull(claimsBefore);

            Assert.False(isValidAfterExpiration, "Token should be invalid just after expiration");
            Assert.Null(claimsAfter);
        }
    }
}
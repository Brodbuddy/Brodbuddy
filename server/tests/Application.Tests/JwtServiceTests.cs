using System.Text.Json;
using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedTestDependencies;
using Shouldly;
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
            _jwtService.TryValidate(token, out var decodedClaims).ShouldBeTrue();
            decodedClaims.ShouldNotBeNull();
            decodedClaims.Iss.ShouldBe(_options.Jwt.Issuer);
            decodedClaims.Aud.ShouldBe(_options.Jwt.Audience);
            decodedClaims.Sub.ShouldBe(subject);
            decodedClaims.Email.ShouldBe(email);
            decodedClaims.Role.ShouldBe(role);
            decodedClaims.Exp.ShouldBeGreaterThan(decodedClaims.Iat);
        }

        [Fact]
        public void Generate_ShouldAddIssuedAtClaim_WithCorrectTimestamp()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";
            var expectedIssueTime = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

            // Act
            var token = _jwtService.Generate(subject, email, role);

            // Assert
            _jwtService.TryValidate(token, out var decodedClaims).ShouldBeTrue();
            decodedClaims.ShouldNotBeNull();
            decodedClaims.Iat.ShouldBe(expectedIssueTime);
        }

        [Fact]
        public void Generate_ShouldAddUniqueJtiClaim_ForEachToken()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";

            // Act
            var token1 = _jwtService.Generate(subject, email, role);
            var token2 = _jwtService.Generate(subject, email, role);

            // Assert
            _jwtService.TryValidate(token1, out var decodedClaims1).ShouldBeTrue();
            _jwtService.TryValidate(token2, out var decodedClaims2).ShouldBeTrue();

            decodedClaims1.ShouldNotBeNull();
            decodedClaims2.ShouldNotBeNull();

            decodedClaims1.Jti.ShouldNotBeNullOrEmpty();
            decodedClaims2.Jti.ShouldNotBeNullOrEmpty();

            decodedClaims1.Jti.ShouldNotBe(decodedClaims2.Jti, "Each token should have a unique JTI");


            Guid.TryParse(decodedClaims1.Jti, out _).ShouldBeTrue("JTI should be a valid GUID");
            Guid.TryParse(decodedClaims2.Jti, out _).ShouldBeTrue("JTI should be a valid GUID");
        }

        [Fact]
        public void Generate_ShouldAddCorrectTypeHeader()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";

            // Act
            var token = _jwtService.Generate(subject, email, role);

            // Assert
            var parts = token.Split('.');
            parts.Length.ShouldBe(3);

            // Ekstraher header (første del af JWT)
            var headerBase64 = parts[0];

            // Udregn padding der mangles (JWT bruger base64url hvilket udelader padding)
            int paddingNeeded = (4 - headerBase64.Length % 4) % 4;

            // Tilføj reel base64 padding karakterer
            string properlyPaddedBase64 = headerBase64.PadRight(headerBase64.Length + paddingNeeded, '=');

            // Decode fra base64 til binær
            byte[] headerBytes = Convert.FromBase64String(properlyPaddedBase64);

            // Konverter binær til JSON string
            string headerJson = System.Text.Encoding.UTF8.GetString(headerBytes);

            // Parse JSON til dictionary
            var header = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson);

            header.ShouldNotBeNull("Deserialized header should not be null");
            header.ShouldContainKey("typ");
            header["typ"].GetString().ShouldBe("JWT");
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
            isValid.ShouldBeFalse();
            claims.ShouldBeNull();
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
            isValid.ShouldBeFalse();
            claims.ShouldBeNull();
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
            isValidBeforeExpiration.ShouldBeTrue("Token should be valid just before expiration");
            claimsBefore.ShouldNotBeNull();

            isValidAfterExpiration.ShouldBeFalse("Token should be invalid just after expiration");
            claimsAfter.ShouldBeNull();
        }

        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenIssuerDoesNotMatch()
        {
            // Arrange
            var modifiedOptions = new AppOptions
            {
                Jwt = new JwtOptions
                {
                    Secret = _options.Jwt.Secret,
                    Issuer = "wrong-issuer",
                    Audience = _options.Jwt.Audience,
                    ExpirationMinutes = _options.Jwt.ExpirationMinutes
                }
            };

            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(modifiedOptions);

            var jwtServiceWithWrongIssuer = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = _jwtService.Generate("user123", "test@example.com", "User");

            // Act
            var isValid = jwtServiceWithWrongIssuer.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Token should be invalid when issuer doesn't match");
            claims.ShouldBeNull();
        }

        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenAudienceDoesNotMatch()
        {
            // Arrange
            var modifiedOptions = new AppOptions
            {
                Jwt = new JwtOptions
                {
                    Secret = _options.Jwt.Secret,
                    Issuer = _options.Jwt.Issuer,
                    Audience = "wrong-audience",
                    ExpirationMinutes = _options.Jwt.ExpirationMinutes
                }
            };

            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(modifiedOptions);

            var jwtServiceWithWrongAudience = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = _jwtService.Generate("user123", "test@example.com", "User");

            // Act
            var isValid = jwtServiceWithWrongAudience.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Token should be invalid when audience doesn't match");
            claims.ShouldBeNull();
        }


        [Theory]
        [InlineData(typeof(InvalidOperationException), "Simulated error")]
        [InlineData(typeof(Exception), "Test generic exception")]
        public void TryValidate_ShouldReturnFalse_WhenExceptionOccurs(Type exceptionType, string exceptionMessage)
        {
            // Arrange
            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue)
                .Throws((Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!);

            var jwtServiceWithException = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = "valid.looking.token";

            // Act
            var isValid = jwtServiceWithException.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse($"Should gracefully handle {exceptionType.Name}");
            claims.ShouldBeNull();
        }


        [Fact]
        public void TryValidate_ShouldLogWarning_WhenSignatureVerificationFails()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";

            var mockLogger = new Mock<ILogger<JwtService>>();
            var jwtService = new JwtService(
                Mock.Of<IOptionsMonitor<AppOptions>>(o => o.CurrentValue == _options),
                _timeProvider,
                mockLogger.Object);

            var token = _jwtService.Generate(subject, email, role);
            var parts = token.Split('.');

            var invalidSignature = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var tamperedToken = $"{parts[0]}.{parts[1]}.{invalidSignature}";

            // Act
            var isValid = jwtService.TryValidate(tamperedToken, out var claims);

            // Assert
            isValid.ShouldBeFalse("Tampered token should be invalid");
            claims.ShouldBeNull();
        }


        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenSecretKeyIsInvalid()
        {
            // Arrange
            var modifiedOptions = new AppOptions
            {
                Jwt = new JwtOptions
                {
                    Secret = "invalid-secret-key",
                    Issuer = _options.Jwt.Issuer,
                    Audience = _options.Jwt.Audience,
                    ExpirationMinutes = _options.Jwt.ExpirationMinutes
                }
            };

            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(modifiedOptions);

            var jwtServiceWithInvalidKey = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = _jwtService.Generate("user123", "test@example.com", "User");

            // Act
            var isValid = jwtServiceWithInvalidKey.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Token should be invalid when using incorrect secret key");
            claims.ShouldBeNull();
        }

        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenTokenHasBeenTampered()
        {
            // Arrange
            var subject = "user123";
            var email = "test@example.com";
            var role = "User";

            // Generate a valid token
            var validToken = _jwtService.Generate(subject, email, role);

            // Opdel tokenet til dets tre dele
            var parts = validToken.Split('.');

            // Ekstraher det encoded payload (anden del)
            var encodedPayload = parts[1];

            // Tilføj det krævede Base64 padding (JWT bruger Base64URL uden padding)
            int paddingNeeded = (4 - encodedPayload.Length % 4) % 4;
            string paddedPayload = encodedPayload.PadRight(encodedPayload.Length + paddingNeeded, '=');

            // Decode payload fra Base64 til string
            byte[] payloadBytes = Convert.FromBase64String(paddedPayload);
            string payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

            // Manipuler med payload ved at ændre email
            string tamperedPayloadJson = payloadJson.Replace(email, "hacker@evil.com");

            // Re-encode det manipulerde payload til Base64URL format
            string tamperedPayloadBase64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(tamperedPayloadJson))
                .TrimEnd('=') // Fjern padding
                .Replace('+', '-') // Gør URL safe
                .Replace('/', '_'); // Gør URL safe

            // Genskab tokenet med det manipulerede payload
            string tamperedToken = $"{parts[0]}.{tamperedPayloadBase64}.{parts[2]}";

            // Act
            var isValid = _jwtService.TryValidate(tamperedToken, out var claims);

            // Assert
            isValid.ShouldBeFalse("Tampered token should be detected as invalid");
            claims.ShouldBeNull();
        }
    }
}
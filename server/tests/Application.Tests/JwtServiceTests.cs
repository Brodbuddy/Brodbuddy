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

            var headerBase64 = parts[0];
            var headerJson = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(headerBase64.PadRight(headerBase64.Length + (4 - headerBase64.Length % 4) % 4, '=')));

            var header = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson);
    
            header.ShouldNotBeNull("Deserialized header should not be null");
            header.ShouldContainKey("typ");
            header["typ"].GetString().ShouldBe("JWT");
        }

        [Fact]
        public void Generate_ShouldAddCorrectHeaderType_ForJwtToken()
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

            token.ShouldNotBeNullOrEmpty();
            token.Split('.').Length.ShouldBe(3, "JWT should have 3 parts: header.payload.signature");
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
        

        [Fact]
        public void TryValidate_ShouldHandleGenericExceptions()
        {
            // Arrange
            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Throws(new InvalidOperationException("Simulated error"));

            var jwtServiceWithException = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = "valid.looking.token";

            // Act
            var isValid = jwtServiceWithException.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Should gracefully handle exceptions");
            claims.ShouldBeNull();
        }


        [Fact]
        public void TryValidate_ShouldLogWarning_WhenTokenIsExpired()
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

            // Advance time past expiration
            _timeProvider.Advance(TimeSpan.FromMinutes(16));

            // Act
            var isValid = jwtService.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Token should be invalid when expired");
            claims.ShouldBeNull();

            
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Token expired")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
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
    
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Signature verification failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public void TryValidate_ShouldLogError_OnGenericException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<JwtService>>();
            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Throws(new Exception("Test generic exception"));

            var jwtService = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                mockLogger.Object);

            var token = "some.token.value";

            // Act
            var isValid = jwtService.TryValidate(token, out var claims);

            // Assert
            isValid.ShouldBeFalse("Should return false on generic exception");
            claims.ShouldBeNull();

            
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("An error occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        
        [Fact]
        public void TryValidate_ShouldReturnFalse_WhenGenericExceptionOccurs()
        {
            // Arrange
            var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
            mockOptionsMonitor.Setup(x => x.CurrentValue).Throws(new Exception("Test generic exception"));

            var jwtService = new JwtService(
                mockOptionsMonitor.Object,
                _timeProvider,
                Mock.Of<ILogger<JwtService>>());

            var token = "some.token.value";

            // Act
            var result = jwtService.TryValidate(token, out var claims);

            // Assert
            result.ShouldBeFalse("Should return false on generic exception");
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
            
            var parts = validToken.Split('.');
            var payload = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));
            
            var tamperedPayload = payload.Replace(email, "hacker@evil.com");
            
            var tamperedPayloadBase64 = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(tamperedPayload))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            
            var tamperedToken = $"{parts[0]}.{tamperedPayloadBase64}.{parts[2]}";
    
            // Act
            var isValid = _jwtService.TryValidate(tamperedToken, out var claims);
    
            // Assert
            isValid.ShouldBeFalse("Tampered token should be detected as invalid");
            claims.ShouldBeNull();
        }
        
    }
    

    public class TimeProviderAdapterTests : JwtServiceTests
    {
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenTimeProviderIsNull()
        {
            // Arrange
            TimeProvider? nullTimeProvider = null;

            // Act & Assert
            var exception = Should.Throw<ArgumentNullException>(() =>
                new TimeProviderAdapter(nullTimeProvider!));

            exception.ParamName.ShouldBe("timeProvider");

            var validProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var adapter = new TimeProviderAdapter(validProvider);
            adapter.ShouldNotBeNull();
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WithCorrectParameterName_WhenTimeProviderIsNull()
        {
            // Arrange
            TimeProvider? nullTimeProvider = null;

            // Act & Assert
            var exception = Should.Throw<ArgumentNullException>(() =>
                new TimeProviderAdapter(nullTimeProvider!));

            exception.ParamName.ShouldBe("timeProvider");

            exception.Message.ShouldContain("timeProvider");
        }

        [Fact]
        public void Constructor_MustRequireNonNullTimeProvider()
        {
            // Arrange
            TimeProvider? nullTimeProvider = null;

            // Act & Assert
            Should.Throw<ArgumentNullException>(() =>
                    new TimeProviderAdapter(nullTimeProvider!))
                .ParamName.ShouldBe("timeProvider");

            var validTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
            var adapter = new TimeProviderAdapter(validTimeProvider);

            adapter.ShouldNotBeNull();
        }

        [Fact]
        public void GetNow_ShouldReturnValueFromTimeProvider()
        {
            // Arrange
            var fixedTime = new DateTimeOffset(2025, 4, 5, 12, 30, 45, TimeSpan.Zero);
            var fakeTimeProvider = new FakeTimeProvider(fixedTime);
            var adapter = new TimeProviderAdapter(fakeTimeProvider);

            // Act
            var result = adapter.GetNow();

            // Assert
            result.ShouldBe(fixedTime);
        }

        [Fact]
        public void GetNow_ShouldReflectChangesInTimeProvider()
        {
            // Arrange
            var initialTime = new DateTimeOffset(2025, 4, 5, 12, 30, 45, TimeSpan.Zero);
            var fakeTimeProvider = new FakeTimeProvider(initialTime);
            var adapter = new TimeProviderAdapter(fakeTimeProvider);

            // Act
            var initialResult = adapter.GetNow();

            var timeAdvance = TimeSpan.FromHours(2);
            fakeTimeProvider.Advance(timeAdvance);

            var advancedResult = adapter.GetNow();

            // Assert
            initialResult.ShouldBe(initialTime);
            advancedResult.ShouldBe(initialTime.Add(timeAdvance));
        }
    }
    }
        
        
        
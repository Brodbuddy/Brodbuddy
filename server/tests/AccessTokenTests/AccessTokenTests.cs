using Application;
using Application.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace AccessTokenTests;

public class AccessTokenTests
{
    private readonly AccessToken _accessToken;

    public AccessTokenTests()
    {
        var options = new AppOptions {JwtSecret = "testsecretkeywithenoughlengthforsecurity1234567890" };
        var mockOptionsMonitor = new Mock<IOptionsMonitor<AppOptions>>();
        mockOptionsMonitor.Setup(x => x.CurrentValue).Returns(options);

        var fixedTime = new DateTimeOffset(2025, 4, 2, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(fixedTime);

        var optionsMonitor = mockOptionsMonitor.Object;
        _accessToken = new AccessToken(optionsMonitor, timeProvider);
    }

    [Fact]
    public void Generate_ShouldRespectExistingExpiration()
    {
        // Arrange
        var specificExp = "1767193600";
        var claims = new JwtClaims
        {
            Email = "test@test.dk",
            Id = "25",
            Role = "Admin",
            Exp = specificExp
        };

        // Act
        var token = _accessToken.Generate(claims);
        var decodedClaims = _accessToken.Validate(token);

        // Assert
        Assert.Equal(specificExp, decodedClaims.Exp);
    }

    [Fact]
    public void Validate_ShouldThrowException_WhenTokenFormatIsInvalid()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act & Assert
        Assert.Throws<FormatException>(() =>
            _accessToken.Validate(invalidToken));
    }
    
    [Fact]
    public void Generate_ShouldSetDefaultExpiration_WhenNoExpirationProvided()
    {
        // Arrange
        var claims = new JwtClaims
        {
            Email = "test@test.dk",
            Id = "25",
            Role = "Admin",
            Exp = null 
        };

        // Act
        var token = _accessToken.Generate(claims);
        var decodedClaims = _accessToken.Validate(token);

        // Assert
        Assert.NotNull(decodedClaims.Exp);
    }
    
    
}
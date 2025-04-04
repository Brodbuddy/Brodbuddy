using Application;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace RefreshTokenTests;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repositoryMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<RefreshTokenService>> _loggerMock;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        _repositoryMock = new Mock<IRefreshTokenRepository>();
        _timeProviderMock = new Mock<TimeProvider>();
        _loggerMock = new Mock<ILogger<RefreshTokenService>>();
        
        
        _service = new RefreshTokenService(_repositoryMock.Object, _timeProviderMock.Object, _loggerMock.Object);
        
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnToken()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(new DateTimeOffset(utcNow));

        // Act
        var token = await _service.GenerateAsync();

        // Assert
        Assert.False(string.IsNullOrEmpty(token));
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<string>(), utcNow.AddDays(30)), Times.Once);
    }

    [Fact]
    public async Task TryValidateAsync_ShouldReturnFalse_WhenTokenIsNullOrEmpty()
    {
        // Act
        var result = await _service.TryValidateAsync(string.Empty); 

        // Assert
        Assert.False(result.isValid);
        Assert.Equal(Guid.Empty, result.tokenId);
    }

    
    
    [Fact]
    public async Task TryValidateAsync_ShouldReturnResultFromRepository()
    {
        // Arrange
        var token = "testToken";
        var expectedResult = (true, Guid.NewGuid());
        _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync(expectedResult);

        // Act
        var result = await _service.TryValidateAsync(token);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task RevokeAsync_ShouldReturnFalse_WhenTokenIsNullOrEmpty()
    {
        // Act
        var result = await _service.RevokeAsync(string.Empty); 

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeAsync_ShouldReturnFalse_WhenTokenIsInvalid()
    {
        // Arrange
        var token = "invalidToken";
        _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));

        // Act
        var result = await _service.RevokeAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RevokeAsync_ShouldReturnTrue_WhenTokenIsValid()
    {
        // Arrange
        var token = "validToken";
        var tokenId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
        _repositoryMock.Setup(r => r.RevokeAsync(tokenId)).ReturnsAsync(true);

        // Act
        var result = await _service.RevokeAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RotateAsync_ShouldReturnEmptyString_WhenTokenIsNullOrEmpty()
    {
        // Act
        var result = await _service.RotateAsync(string.Empty); // Ændret fra null til string.Empty

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task RotateAsync_ShouldReturnEmptyString_WhenTokenIsInvalid()
    {
        // Arrange
        var token = "invalidToken";
        _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));

        // Act
        var result = await _service.RotateAsync(token);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task RotateAsync_ShouldReturnNewToken_WhenTokenIsValid()
    {
        // Arrange
        var token = "validToken";
        var tokenId = Guid.NewGuid();
        var newToken = "newToken";
        _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
        _repositoryMock.Setup(r => r.RotateAsync(tokenId)).ReturnsAsync(newToken);

        // Act
        var result = await _service.RotateAsync(token);

        // Assert
        Assert.Equal(newToken, result);
    }

  
}
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repositoryMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly Mock<ILogger<RefreshTokenService>> _loggerMock;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        var repositoryMock = new Mock<IRefreshTokenRepository>();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _loggerMock = new Mock<ILogger<RefreshTokenService>>();
        _service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
    }
    
    public class GenerateAsync : RefreshTokenServiceTests
    {
         [Fact]
            public async Task GenerateAsync_ShouldReturnToken()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var utcNow = DateTime.UtcNow;
                var timeProvider = new FakeTimeProvider(new DateTimeOffset(utcNow));
                var service = new RefreshTokenService(repositoryMock.Object, timeProvider, _loggerMock.Object);
        
                // Act
                var token = await service.GenerateAsync();
        
                // Assert
                Assert.False(string.IsNullOrEmpty(token));
                repositoryMock.Verify(r => r.CreateAsync(It.IsAny<string>(), utcNow.AddDays(30)), Times.Once);
            }
    }

    
    public class TryValidateAsync : RefreshTokenServiceTests
    {
        [Fact]
            public async Task TryValidateAsync_ShouldReturnResultFromRepository()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "testToken";
                var expectedResult = (true, Guid.NewGuid());
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync(expectedResult);
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.TryValidateAsync(token);
        
                // Assert
                Assert.Equal(expectedResult, result);
            }
    }
   
    public class RevokeAsync : RefreshTokenServiceTests
    {
        [Fact]
            public async Task RevokeAsync_ShouldReturnFalse_WhenTokenIsInvalid()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "invalidToken";
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.RevokeAsync(token);
        
                // Assert
                Assert.False(result);
            }
        
            [Fact]
            public async Task RevokeAsync_ShouldReturnTrue_WhenTokenIsValid()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "validToken";
                var tokenId = Guid.NewGuid();
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
                repositoryMock.Setup(r => r.RevokeAsync(tokenId)).ReturnsAsync(true);
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.RevokeAsync(token);
        
                // Assert
                Assert.True(result);
            }

    }
    
    
    public class RotateAsync : RefreshTokenServiceTests
    {
         [Fact]
            public async Task RotateAsync_ShouldReturnEmptyString_WhenTokenIsInvalid()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "invalidToken";
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.RotateAsync(token);
        
                // Assert
                Assert.Equal(string.Empty, result);
            }
        
            [Fact]
            public async Task RotateAsync_ShouldReturnNewToken_WhenTokenIsValid()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "validToken";
                var tokenId = Guid.NewGuid();
                var newToken = "newToken";
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
                repositoryMock.Setup(r => r.RotateAsync(tokenId)).ReturnsAsync(newToken);
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.RotateAsync(token);
        
                // Assert
                Assert.Equal(newToken, result);
            }
        
            [Fact]
            public async Task RotateAsync_ShouldReturnEmptyString_WhenInvalidOperationExceptionIsThrown()
            {
                // Arrange
                var repositoryMock = new Mock<IRefreshTokenRepository>();
                var token = "validToken";
                var tokenId = Guid.NewGuid();
                repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
                repositoryMock.Setup(r => r.RotateAsync(tokenId))
                    .ThrowsAsync(new InvalidOperationException("Token rotation failed"));
                var service = new RefreshTokenService(repositoryMock.Object, _timeProvider, _loggerMock.Object);
        
                // Act
                var result = await service.RotateAsync(token);
        
                // Assert
                Assert.Equal(string.Empty, result);
               
            }
    }

    
   
}
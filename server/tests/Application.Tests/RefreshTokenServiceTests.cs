using Application.Interfaces;
using Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using SharedTestDependencies;
using Shouldly;
using Xunit;

namespace Application.Tests;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repositoryMock;
    private readonly Mock<ILogger<RefreshTokenService>> _loggerMock;
    private readonly RefreshTokenService _service;
    private readonly FakeTimeProvider _timeProvider;

    public RefreshTokenServiceTests()
    {
        _repositoryMock = new Mock<IRefreshTokenRepository>();
        _loggerMock = new Mock<ILogger<RefreshTokenService>>();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _service = new RefreshTokenService(_repositoryMock.Object, _timeProvider, _loggerMock.Object);
    }

    public class GenerateAsync : RefreshTokenServiceTests
    {
        [Fact]
        public async Task GenerateAsync_ShouldReturnToken()
        {
            // Arrange
            var utcNow = DateTime.UtcNow;
            
            _timeProvider.SetUtcNow(utcNow);
    
            // Act
            var token = await _service.GenerateAsync();
    
            // Assert
            token.ShouldNotBeNullOrEmpty();
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<string>(), utcNow.AddDays(30)), Times.Once);
        }
    }

    public class TryValidateAsync : RefreshTokenServiceTests
    {
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
            result.ShouldBe(expectedResult);
        }
    }

    public class RevokeAsync : RefreshTokenServiceTests
    {
        [Fact]
        public async Task RevokeAsync_ShouldReturnFalse_WhenTokenIsInvalid()
        {
            // Arrange
            var token = "invalidToken";
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));

            // Act
            var result = await _service.RevokeAsync(token);

            // Assert
            result.ShouldBeFalse();
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
            result.ShouldBeTrue();
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RevokeAsync_ShouldReturnFalse_WhenTokenIsNullOrEmpty(string token)
        {
            // Act
            var result = await _service.RevokeAsync(token);

            // Assert
            result.ShouldBeFalse();
            _repositoryMock.Verify(r => r.TryValidateAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.RevokeAsync(It.IsAny<Guid>()), Times.Never);
        }
    }

    public class RotateAsync : RefreshTokenServiceTests
    {
        [Fact]
        public async Task RotateAsync_ShouldReturnEmptyString_WhenTokenIsInvalid()
        {
            // Arrange
            var token = "invalidToken";
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((false, Guid.Empty));

            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.ShouldBe(string.Empty);
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
            result.ShouldBe(newToken);
        }

        [Fact]
        public async Task RotateAsync_ShouldReturnEmptyString_WhenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var token = "validToken";
            var tokenId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
            _repositoryMock.Setup(r => r.RotateAsync(tokenId))
                .ThrowsAsync(new InvalidOperationException("Token rotation failed"));

            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task RotateAsync_ShouldLogWarning_WhenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var token = "validToken";
            var tokenId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
            _repositoryMock.Setup(r => r.RotateAsync(tokenId))
                .ThrowsAsync(new InvalidOperationException("Token rotation failed"));

            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.ShouldBe(string.Empty);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) =>
                        o.ToString().Contains($"Failed to rotate refresh token. Token ID: {tokenId}")),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()), 
                Times.Once);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task RotateAsync_ShouldReturnEmptyString_WhenTokenIsNullOrEmpty(string token)
        {
            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.ShouldBe(string.Empty);
            _repositoryMock.Verify(r => r.TryValidateAsync(It.IsAny<string>()), Times.Never);
            _repositoryMock.Verify(r => r.RotateAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
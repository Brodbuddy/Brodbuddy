using Application.Interfaces.Data.Repositories;
using Application.Services;
using Moq;
using SharedTestDependencies.Fakes;
using Shouldly;
using Xunit;

namespace Application.Tests.Services;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _repositoryMock;

    private readonly RefreshTokenService _service;
    private readonly FakeTimeProvider _timeProvider;

    protected RefreshTokenServiceTests()
    {
        _repositoryMock = new Mock<IRefreshTokenRepository>();

        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _service = new RefreshTokenService(_repositoryMock.Object, _timeProvider);
    }

    public class GenerateAsync : RefreshTokenServiceTests
    {
        [Fact]
        public async Task GenerateAsync_ShouldReturnToken()
        {
            // Arrange

            var utcNow = DateTime.UtcNow;

            var expectedTokenId = Guid.NewGuid();
            var testToken = "token";

            var timeProvider = new FakeTimeProvider(new DateTimeOffset(utcNow));
            var service = new RefreshTokenService(_repositoryMock.Object, timeProvider);

            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<string>(), utcNow.AddDays(30)))
                .ReturnsAsync((testToken, expectedTokenId));

            // Act
            var result = await service.GenerateAsync();

            // Assert
            result.token.ShouldNotBeNullOrEmpty();
            result.tokenId.ShouldBe(expectedTokenId);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<string>(), utcNow.AddDays(30)), Times.Once);
        }
    }

    public class TryValidateAsync : RefreshTokenServiceTests
    {
        [Fact]
        public async Task TryValidateAsync_ShouldReturnValidationResult()
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
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task TryValidateAsync_WithNullOrEmptyToken_ShouldReturnInvalidResult(string? token)
        {
            // Act
            var result = await _service.TryValidateAsync(token!);

            // Assert
            result.isValid.ShouldBeFalse();
            result.tokenId.ShouldBe(Guid.Empty);

            _repositoryMock.Verify(r => r.TryValidateAsync(It.IsAny<string>()), Times.Never);
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
        [InlineData("   ")]
        public async Task RevokeAsync_WithNullOrEmptyToken_ShouldReturnFalse(string? token)
        {
            // Act
            var result = await _service.RevokeAsync(token!);

            // Assert
            result.ShouldBeFalse();
            
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
            result.token.ShouldBe(string.Empty);
            result.tokenId.ShouldBe(Guid.Empty);
        }

        [Fact]
        public async Task RotateAsync_ShouldReturnNewToken_WhenTokenIsValid()
        {
            // Arrange
            var token = "validToken";
            var tokenId = Guid.NewGuid();
            var newToken = "newToken";
            var newTokenId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
            _repositoryMock.Setup(r => r.RotateAsync(tokenId)).ReturnsAsync((newToken, newTokenId));

            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.token.ShouldBe(newToken);
            result.tokenId.ShouldBe(newTokenId);
        }

        [Fact]
        public async Task RotateAsync_ShouldReturnEmptyString_WhenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var token = "validToken";
            var tokenId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.TryValidateAsync(token)).ReturnsAsync((true, tokenId));
            _repositoryMock.Setup(r => r.RotateAsync(tokenId)).ThrowsAsync(new InvalidOperationException("Token rotation failed"));

            // Act
            var result = await _service.RotateAsync(token);

            // Assert
            result.token.ShouldBe(string.Empty);
            result.tokenId.ShouldBe(Guid.Empty);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task RotateAsync_WithNullOrEmptyToken_ShouldReturnEmptyResult(string? token)
        {
            // Act
            var result = await _service.RotateAsync(token!);

            // Assert
            result.token.ShouldBe(string.Empty);
            result.tokenId.ShouldBe(Guid.Empty);
            
            _repositoryMock.Verify(r => r.RotateAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
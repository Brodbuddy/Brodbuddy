using Api.Websocket.Auth;
using Application.Interfaces.Auth;
using Application.Models;
using Brodbuddy.WebSocket.Auth;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Api.Websocket.Tests.Auth;

public class JwtWebSocketAuthHandlerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _scopedServiceProviderMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly Mock<ILogger<JwtWebSocketAuthHandler>> _loggerMock;
    private readonly Mock<IWebSocketConnection> _connectionMock;
    private readonly JwtWebSocketAuthHandler _handler;

    protected JwtWebSocketAuthHandlerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _scopedServiceProviderMock = new Mock<IServiceProvider>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _loggerMock = new Mock<ILogger<JwtWebSocketAuthHandler>>();
        _connectionMock = new Mock<IWebSocketConnection>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(s => s.ServiceProvider).Returns(_scopedServiceProviderMock.Object);
        _scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService))).Returns(_authServiceMock.Object);

        _handler = new JwtWebSocketAuthHandler(
            _serviceProviderMock.Object,
            _loggerMock.Object);
    }

    public class AuthenticateAsync : JwtWebSocketAuthHandlerTests
    {
        [Fact]
        public async Task AuthenticateAsync_WithValidToken_ReturnsSuccessResult()
        {
            // Arrange
            const string token = "valid-token";
            const string userId = "user-123";
            var roles = new[] { "member", "admin" };
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(token)).ReturnsAsync(new AuthenticationResult(true, userId, roles));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeTrue();
            result.UserId.ShouldBe(userId);
            result.Roles.ShouldBe(roles);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidToken_ReturnsFailureResult()
        {
            // Arrange
            const string token = "invalid-token";
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(token)).ReturnsAsync(new AuthenticationResult(false));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeFalse();
            result.UserId.ShouldBeNull();
            result.Roles.ShouldBeEmpty();
        }

        [Fact]
        public async Task AuthenticateAsync_WithBearerPrefix_StripsPrefix()
        {
            // Arrange
            const string tokenWithBearer = "Bearer valid-token";
            const string expectedToken = "valid-token";
            const string userId = "user-123";
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(expectedToken)).ReturnsAsync(new AuthenticationResult(true, userId, ["member"]));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, tokenWithBearer, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeTrue();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(expectedToken), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithBearerPrefixDifferentCase_StripsPrefix()
        {
            // Arrange
            const string tokenWithBearer = "bearer valid-token";
            const string expectedToken = "valid-token";
            const string userId = "user-123";
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(expectedToken)).ReturnsAsync(new AuthenticationResult(true, userId, ["member"]));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, tokenWithBearer, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeTrue();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(expectedToken), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithWhitespaceAfterBearer_TrimsToken()
        {
            // Arrange
            const string tokenWithBearer = "Bearer   valid-token   ";
            const string expectedToken = "valid-token";
            const string userId = "user-123";
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(expectedToken)).ReturnsAsync(new AuthenticationResult(true, userId, ["member"]));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, tokenWithBearer, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeTrue();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(expectedToken), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithNullToken_ReturnsFailureResult()
        {
            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, null, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeFalse();
            result.UserId.ShouldBeNull();
            result.Roles.ShouldBeEmpty();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyToken_ReturnsFailureResult()
        {
            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, string.Empty, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeFalse();
            result.UserId.ShouldBeNull();
            result.Roles.ShouldBeEmpty();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_WithWhitespaceOnlyToken_ReturnsFailureResult()
        {
            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, "   ", "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeFalse();
            result.UserId.ShouldBeNull();
            result.Roles.ShouldBeEmpty();
            _authServiceMock.Verify(s => s.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_CreatesNewScopeForEachCall()
        {
            // Arrange
            const string token = "valid-token";
            _authServiceMock.Setup(s => s.ValidateTokenAsync(token)).ReturnsAsync(new AuthenticationResult(true, "user-123", ["member"]));

            // Act
            await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message-1");
            await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message-2");

            // Assert
            _scopeFactoryMock.Verify(sf => sf.CreateScope(), Times.Exactly(2));
            _scopeMock.Verify(s => s.Dispose(), Times.Exactly(2));
        }

        [Fact]
        public async Task AuthenticateAsync_PassesCorrectTokenToAuthService()
        {
            // Arrange
            const string originalToken = "test-token-12345";
            _authServiceMock.Setup(s => s.ValidateTokenAsync(originalToken)).ReturnsAsync(new AuthenticationResult(false));

            // Act
            await _handler.AuthenticateAsync(_connectionMock.Object, originalToken, "test-message");

            // Assert
            _authServiceMock.Verify(s => s.ValidateTokenAsync(originalToken), Times.Once);
        }

        [Fact]
        public async Task AuthenticateAsync_WithMultipleRoles_PreservesAllRoles()
        {
            // Arrange
            const string token = "valid-token";
            var roles = new[] { "member", "admin", "moderator" };
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(token)).ReturnsAsync(new AuthenticationResult(true, "user-123", roles));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message");

            // Assert
            result.Roles.Count.ShouldBe(3);
            result.Roles.ShouldContain("member");
            result.Roles.ShouldContain("admin");
            result.Roles.ShouldContain("moderator");
        }

        [Fact]
        public async Task AuthenticateAsync_WithEmptyRolesArray_ReturnsEmptyRoles()
        {
            // Arrange
            const string token = "valid-token";
            
            _authServiceMock.Setup(s => s.ValidateTokenAsync(token)).ReturnsAsync(new AuthenticationResult(true, "user-123", Array.Empty<string>()));

            // Act
            var result = await _handler.AuthenticateAsync(_connectionMock.Object, token, "test-message");

            // Assert
            result.IsAuthenticated.ShouldBeTrue();
            result.Roles.ShouldBeEmpty();
        }
    }
}
using System.Text.Json;
using Api.Websocket.Middleware;
using Application.Models;
using Application.Services;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit.Abstractions;

namespace Api.Websocket.Tests.Middleware;

public class FeatureToggleWebSocketMiddlewareTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<FeatureToggleWebSocketMiddleware>> _loggerMock;
    private readonly Mock<IFeatureToggleService> _toggleServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IWebSocketConnection> _socketMock;
    private readonly FeatureToggleWebSocketMiddleware _middleware;

    private FeatureToggleWebSocketMiddlewareTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<FeatureToggleWebSocketMiddleware>>();
        _toggleServiceMock = new Mock<IFeatureToggleService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _socketMock = new Mock<IWebSocketConnection>();

        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IFeatureToggleService)))
            .Returns(_toggleServiceMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IJwtService)))
            .Returns(_jwtServiceMock.Object);
        
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        _scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);
        
        _middleware = new FeatureToggleWebSocketMiddleware(_scopeFactoryMock.Object, _loggerMock.Object);
    }

    public class InvokeAsync(ITestOutputHelper outputHelper) : FeatureToggleWebSocketMiddlewareTests(outputHelper)
    {
        [Fact]
        public async Task InvokeAsync_WithInvalidJson_CallsNext()
        {
            // Arrange
            var message = "invalid json";
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeTrue();
            nextCalled.ShouldBeTrue();
            _socketMock.Verify(s => s.Send(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WithoutMessageType_CallsNext()
        {
            // Arrange
            var message = JsonSerializer.Serialize(new { Data = "test" });
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeTrue();
            nextCalled.ShouldBeTrue();
            _socketMock.Verify(s => s.Send(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WithEnabledFeatureNoAuth_CallsNext()
        {
            // Arrange
            var message = JsonSerializer.Serialize(new { Type = "TestMessage", RequestId = "123" });
            _toggleServiceMock.Setup(s => s.IsEnabled("Websocket.TestMessage")).Returns(true);
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeTrue();
            nextCalled.ShouldBeTrue();
            _toggleServiceMock.Verify(s => s.IsEnabled("Websocket.TestMessage"), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithDisabledFeatureNoAuth_SendsErrorAndReturnsFalse()
        {
            // Arrange
            var message = JsonSerializer.Serialize(new { Type = "TestMessage", RequestId = "123" });
            _toggleServiceMock.Setup(s => s.IsEnabled("Websocket.TestMessage")).Returns(false);
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeFalse();
            nextCalled.ShouldBeFalse();
            _socketMock.Verify(s => s.Send(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithValidTokenAndUserEnabled_CallsNext()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var message = JsonSerializer.Serialize(new 
            { 
                Type = "TestMessage", 
                Token = "Bearer valid-token",
                RequestId = "123"
            });
            
            _jwtServiceMock.Setup(j => j.TryValidate("valid-token", out It.Ref<JwtClaims>.IsAny))
                .Returns((string token, out JwtClaims claims) =>
                {
                    claims = new JwtClaims(userId.ToString(), "issuer", "audience", 0, 0, "jti", "test@example.com", "user");
                    return true;
                });
            
            _toggleServiceMock.Setup(s => s.IsEnabledForUser("Websocket.TestMessage", userId))
                .Returns(true);
            
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeTrue();
            nextCalled.ShouldBeTrue();
            _toggleServiceMock.Verify(s => s.IsEnabledForUser("Websocket.TestMessage", userId), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidToken_FallsBackToGlobalToggle()
        {
            // Arrange
            var message = JsonSerializer.Serialize(new 
            { 
                Type = "TestMessage", 
                Token = "Bearer invalid-token",
                RequestId = "123"
            });
            
            _jwtServiceMock.Setup(j => j.TryValidate("invalid-token", out It.Ref<JwtClaims>.IsAny))
                .Returns(false);
            
            _toggleServiceMock.Setup(s => s.IsEnabled("Websocket.TestMessage"))
                .Returns(true);
            
            var nextCalled = false;
            Func<Task> next = () => { nextCalled = true; return Task.CompletedTask; };

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeTrue();
            nextCalled.ShouldBeTrue();
            _toggleServiceMock.Verify(s => s.IsEnabled("Websocket.TestMessage"), Times.Once);
            _toggleServiceMock.Verify(s => s.IsEnabledForUser(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_ErrorMessageWithDisabledFeature_IncludesRequestId()
        {
            // Arrange
            var requestId = "test-request-123";
            var message = JsonSerializer.Serialize(new { Type = "TestMessage", RequestId = requestId });
            _toggleServiceMock.Setup(s => s.IsEnabled("Websocket.TestMessage")).Returns(false);
            
            string? sentMessage = null;
            _socketMock.Setup(s => s.Send(It.IsAny<string>()))
                .Callback<string>(msg => sentMessage = msg)
                .Returns(Task.CompletedTask);
            
            Func<Task> next = () => Task.CompletedTask;

            // Act
            var result = await _middleware.InvokeAsync(_socketMock.Object, message, next);

            // Assert
            result.ShouldBeFalse();
            sentMessage.ShouldNotBeNull();
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(sentMessage);
            errorResponse.GetProperty("RequestId").GetString().ShouldBe(requestId);
            errorResponse.GetProperty("Type").GetString().ShouldBe("Error");
            errorResponse.GetProperty("Payload").GetProperty("Code").GetString().ShouldBe("FEATURE_DISABLED");
        }
    }
}
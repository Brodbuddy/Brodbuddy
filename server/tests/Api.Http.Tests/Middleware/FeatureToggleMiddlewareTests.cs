using Api.Http.Middleware;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;
using Shouldly;
using System.Security.Claims;
using Xunit;
using Xunit.Abstractions;

namespace Api.Http.Tests.Middleware;

public class FeatureToggleMiddlewareTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IFeatureToggleService> _toggleServiceMock;
    private readonly FeatureToggleMiddleware _middleware;
    private readonly Mock<RequestDelegate> _nextMock;

    private FeatureToggleMiddlewareTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _toggleServiceMock = new Mock<IFeatureToggleService>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new FeatureToggleMiddleware(_nextMock.Object);
    }

    public class InvokeAsync(ITestOutputHelper outputHelper) : FeatureToggleMiddlewareTests(outputHelper)
    {
        [Fact]
        public async Task InvokeAsync_WithoutEndpoint_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.SetEndpoint(null);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            _toggleServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task InvokeAsync_WithoutControllerMetadata_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var endpoint = new Endpoint(c => Task.CompletedTask, new EndpointMetadataCollection(), "Test");
            context.SetEndpoint(endpoint);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            _toggleServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task InvokeAsync_WithEnabledFeatureForAnonymous_CallsNext()
        {
            // Arrange
            var context = CreateContextWithEndpoint("TestController", "TestAction");
            var featureName = "Api.TestController.TestAction";
            _toggleServiceMock.Setup(s => s.IsEnabledAsync(featureName)).ReturnsAsync(true);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            context.Response.StatusCode.ShouldBe(200);
        }

        [Fact]
        public async Task InvokeAsync_WithDisabledFeatureForAnonymous_Returns404()
        {
            // Arrange
            var context = CreateContextWithEndpoint("TestController", "TestAction");
            var featureName = "Api.TestController.TestAction";
            _toggleServiceMock.Setup(s => s.IsEnabledAsync(featureName)).ReturnsAsync(false);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
            context.Response.StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task InvokeAsync_WithEnabledFeatureForAuthenticatedUser_CallsNext()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var context = CreateContextWithEndpoint("TestController", "TestAction", userId);
            var featureName = "Api.TestController.TestAction";
            _toggleServiceMock.Setup(s => s.IsEnabledForUserAsync(featureName, userId)).ReturnsAsync(true);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            context.Response.StatusCode.ShouldBe(200);
        }

        [Fact]
        public async Task InvokeAsync_WithDisabledFeatureForAuthenticatedUser_Returns404()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var context = CreateContextWithEndpoint("TestController", "TestAction", userId);
            var featureName = "Api.TestController.TestAction";
            _toggleServiceMock.Setup(s => s.IsEnabledForUserAsync(featureName, userId)).ReturnsAsync(false);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(It.IsAny<HttpContext>()), Times.Never);
            context.Response.StatusCode.ShouldBe(404);
        }

        [Fact]
        public async Task InvokeAsync_WithAuthenticatedUserButMissingClaim_FallsBackToGeneralToggle()
        {
            // Arrange
            var context = CreateContextWithEndpoint("TestController", "TestAction", null, true);
            var featureName = "Api.TestController.TestAction";
            _toggleServiceMock.Setup(s => s.IsEnabledAsync(featureName)).ReturnsAsync(true);

            // Act
            await _middleware.InvokeAsync(context, _toggleServiceMock.Object);

            // Assert
            _nextMock.Verify(n => n(context), Times.Once);
            _toggleServiceMock.Verify(s => s.IsEnabledAsync(featureName), Times.Once);
            _toggleServiceMock.Verify(s => s.IsEnabledForUserAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        private static DefaultHttpContext CreateContextWithEndpoint(string controller, string action, Guid? userId = null, bool authenticatedWithoutClaim = false)
        {
            var context = new DefaultHttpContext();

            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerName = controller,
                ActionName = action
            };

            var metadataCollection = new EndpointMetadataCollection(actionDescriptor);
            var endpoint = new Endpoint(c => Task.CompletedTask, metadataCollection, "Test");
            context.SetEndpoint(endpoint);

            if (userId.HasValue)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                var identity = new ClaimsIdentity(claims, "Test");
                context.User = new ClaimsPrincipal(identity);
            }
            else if (authenticatedWithoutClaim)
            {
                var identity = new ClaimsIdentity("Test");
                context.User = new ClaimsPrincipal(identity);
            }

            return context;
        }
    }
}
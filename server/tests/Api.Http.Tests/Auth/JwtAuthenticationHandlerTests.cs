using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Models;
using Api.Http.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SharedTestDependencies.Constants;
using Shouldly;
using Xunit.Abstractions;
using IAuthenticationService = Application.Interfaces.Auth.IAuthenticationService;

namespace Api.Http.Tests.Auth;

[CollectionDefinition(TestCollections.HttpApi)]
public class JwtAuthenticationHandlerTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly JwtAuthenticationHandler _handler;
    private readonly DefaultHttpContext _httpContext;
    private static readonly string[] DefaultRole = ["user"];

    public JwtAuthenticationHandlerTests(ITestOutputHelper output)
    {
        _output = output;
        _authServiceMock = new Mock<IAuthenticationService>();

        var options = new Mock<IOptionsMonitor<JwtBearerOptions>>();
        options.Setup(o => o.Get(It.IsAny<string>())).Returns(new JwtBearerOptions());

        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(l => l.CreateLogger(It.IsAny<string>()))
                     .Returns(new Mock<ILogger<JwtAuthenticationHandler>>().Object);

        var encoder = UrlEncoder.Default;

        _handler = new JwtAuthenticationHandler(
            options.Object,
            loggerFactory.Object,
            encoder,
            _authServiceMock.Object);

        _httpContext = new DefaultHttpContext();
        _handler.InitializeAsync(new AuthenticationScheme("Bearer", "Bearer", typeof(JwtAuthenticationHandler)),
            _httpContext).Wait();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldSucceed_WhenTokenIsValid()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "Bearer valid-token";

        _authServiceMock.Setup(m => m.ValidateTokenAsync("valid-token"))
                        .ReturnsAsync(new AuthenticationResult(true, "skumbanan", DefaultRole));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Principal.ShouldNotBeNull();
        
        if (result.Principal?.Identity != null)
        {
            result.Principal.Identity.IsAuthenticated.ShouldBeTrue();
            result.Principal.FindFirstValue(ClaimTypes.NameIdentifier).ShouldBe("skumbanan");
            result.Principal.IsInRole("user").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldReturnNoResult_WhenNoAuthorizationHeaderIsPresent()
    {
        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.None.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldReturnNoResult_WhenAuthHeaderDoesNotStartWithBearer()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "NotBearer token";

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.None.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldFailAuthentication_WhenTokenIsInvalid()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "Bearer invalid-token";

        _authServiceMock.Setup(m => m.ValidateTokenAsync("invalid-token"))
                        .ReturnsAsync(new AuthenticationResult(false));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeFalse();
        result.Failure.ShouldNotBeNull();
        result.Failure?.Message.ShouldBe("Invalid token");
    }

    
    [Fact]
    public async Task HandleAuthenticateAsync_ShouldCreateCorrectClaims_WhenTokenIsValid()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "Bearer valid-token";
        var customRoles = new[] { "user", "admin" };

        _authServiceMock.Setup(m => m.ValidateTokenAsync("valid-token"))
                        .ReturnsAsync(new AuthenticationResult(true, "skumbanan", customRoles));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeTrue();
        result.Principal.ShouldNotBeNull();

        if (result.Principal != null)
        {
            result.Principal.HasClaim(ClaimTypes.NameIdentifier, "skumbanan").ShouldBeTrue();
            result.Principal.IsInRole("user").ShouldBeTrue();
            result.Principal.IsInRole("admin").ShouldBeTrue();

            var identity = result.Principal.Identity as ClaimsIdentity;
            identity?.Claims.Count().ShouldBe(3);
        }
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldTrimToken_WhenTokenHasWhitespace()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "Bearer valid-token   ";

        _authServiceMock.Setup(m => m.ValidateTokenAsync("valid-token"))
                        .ReturnsAsync(new AuthenticationResult(true, "skumbanan", DefaultRole));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeTrue();

        _authServiceMock.Verify(m => m.ValidateTokenAsync("valid-token"), Times.Once);
    }

    [Fact]
    public async Task HandleAuthenticateAsync_ShouldHandleMultipleAuthHeaders_UsingFirstOne()
    {
        // Arrange
        _httpContext.Request.Headers.Authorization = "Bearer valid-token";

        _authServiceMock.Setup(m => m.ValidateTokenAsync("valid-token"))
                        .ReturnsAsync(new AuthenticationResult(true, "skumbanan", DefaultRole));

        // Act
        var result = await _handler.AuthenticateAsync();

        // Assert
        result.Succeeded.ShouldBeTrue();
        
        
        _authServiceMock.Verify(m => m.ValidateTokenAsync("valid-token"), Times.Once);
    }
}
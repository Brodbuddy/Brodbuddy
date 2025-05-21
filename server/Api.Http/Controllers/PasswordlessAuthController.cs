using Api.Http.Extensions;
using Api.Http.Models;
using Api.Http.Utils;
using Application;
using Application.Models.DTOs;
using Application.Services.Auth;
using Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using InitiateLoginRequest = Api.Http.Models.InitiateLoginRequest;
using LoginVerificationRequest = Api.Http.Models.LoginVerificationRequest;

namespace Api.Http.Controllers;

[Route("api/passwordless-auth")]
[ApiController]
public class PasswordlessAuthController : ControllerBase
{
    private readonly IPasswordlessAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AppOptions> _appOptions;
    private const string RefreshTokenCookieName = "refreshToken";

    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService, 
        IJwtService jwtService,
        TimeProvider timeProvider,
        IOptions<AppOptions> appOptions)
    {
        _authService = passwordlessAuthService;
        _jwtService = jwtService;
        _timeProvider = timeProvider;
        _appOptions = appOptions;
    }

    [HttpGet("test-token")]
    [AllowAnonymous]
    public TestTokenResponse TestToken()
    {
        var accessToken = _jwtService.Generate("kakao", "kakao@mælk.dk", "user");
        return new TestTokenResponse(AccessToken: accessToken);
    }

    [HttpPost("initiate")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiateLogin([FromBody] InitiateLoginRequest request)
    {
        var success = await _authService.InitiateLoginAsync(request.Email);

        if (!success) return BadRequest(new { message = "Failed to send verification code" });

        return Ok();
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<LoginVerificationResponse> VerifyCode([FromBody] LoginVerificationRequest request)
    {
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString(); 
        var browser = UserAgentUtils.GetBrowser(userAgent);
        var os = UserAgentUtils.GetOperatingSystem(userAgent);
        
        var (accessToken, refreshToken) = await _authService.CompleteLoginAsync(request.Email, request.Code, new DeviceDetails(browser, os, userAgent, ipAddress));

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, GetRefreshTokenCookieOptions());
        
        return new LoginVerificationResponse(accessToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<RefreshTokenResponse> RefreshToken()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
           
        var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
            
        Response.Cookies.Append(RefreshTokenCookieName, newRefreshToken, GetRefreshTokenCookieOptions());
        return new RefreshTokenResponse(accessToken);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, GetRefreshTokenCookieOptions());
        return Ok(new { message = "Logged out succesfully" });
    }
    
    [HttpGet("user-info")]
    public async Task<UserInfoResponse> UserInfo()
    {
        var userInfo = await _authService.UserInfoAsync(HttpContext.User.GetUserId());
        return new UserInfoResponse(Email: userInfo.email, IsAdmin: userInfo.role == Role.Admin);
    }
    
    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_appOptions.Value.Token.RefreshTokenLifeTimeDays)
    };
}
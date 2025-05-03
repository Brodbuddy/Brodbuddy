using Api.Http.Auth;
using Application;
using Application.Interfaces.Auth;
using Application.Models.Dto;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Http.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PasswordlessAuthController : ControllerBase
{
    private readonly IPasswordlessAuthService _authService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AppOptions> _appOptions;
    private const string RefreshTokenCookieName = "refreshToken";

    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService,
        IDeviceDetectionService deviceDetectionService,
        TimeProvider timeProvider,
        IOptions<AppOptions> appOptions)
    {
        _authService = passwordlessAuthService;
        _deviceDetectionService = deviceDetectionService;
        _timeProvider = timeProvider;
        _appOptions = appOptions;
    }

    [HttpPost("initiate")]
    [AllowAnonymous]
    public async Task<IActionResult> InitiateLogin([FromBody] InitiateLoginRequest request)
    {
        var success = await _authService.InitiateLoginAsync(request.Email);

        if (!success)
            return BadRequest(new { message = "Failed to send verification code" });

        return Ok();
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCode([FromBody] LoginVerificationRequest request)
    {
        try
        {
            var browser = _deviceDetectionService.GetBrowser();
            var os = _deviceDetectionService.GetOperatingSystem();

            var (accessToken, refreshToken) = await _authService.CompleteLoginAsync(
                request.Email,
                request.Code,
                browser,
                os);

            Response.Cookies.Append(RefreshTokenCookieName, refreshToken, GetRefreshTokenCookieOptions());
            
            return Ok(new { accessToken });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid verification code" });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var refreshToken = Request.Cookies[RefreshTokenCookieName];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "No refresh token provided" });
            }
            
            var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
            
            Response.Cookies.Append(RefreshTokenCookieName, newRefreshToken, GetRefreshTokenCookieOptions());
            return Ok(new { accessToken });
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, GetRefreshTokenCookieOptions());
        return Ok(new { message = "Logged out succesfully" });
    }
    
    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_appOptions.Value.Token.RefreshTokenLifeTimeDays)
    };
}
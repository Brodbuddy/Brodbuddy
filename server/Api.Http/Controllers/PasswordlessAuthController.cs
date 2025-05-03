using Application.Interfaces.Auth;
using Application.Models.Dto;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PasswordlessAuthController : ControllerBase
{
    private readonly IPasswordlessAuthService _authService;
    private readonly IDeviceDetectionService _deviceDetectionService;
    private readonly ICookieService _cookieService;

    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService,
        IDeviceDetectionService deviceDetectionService,
        ICookieService cookieService)
    {
        _authService = passwordlessAuthService;
        _deviceDetectionService = deviceDetectionService;
        _cookieService = cookieService;
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

            _cookieService.SetAccessTokenCookie(Response.Cookies, accessToken);
            _cookieService.SetRefreshTokenCookie(Response.Cookies, refreshToken);
            
            return Ok(new { accessToken, refreshToken });
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
            var refreshToken = _cookieService.GetRefreshTokenFromCookies(Request.Cookies);
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized(new { message = "No refresh token provided" });
            }
            
            var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
            
            _cookieService.SetAccessTokenCookie(Response.Cookies, accessToken);
            _cookieService.SetRefreshTokenCookie(Response.Cookies, newRefreshToken);
            return Ok(new { accessToken, newRefreshToken });
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _cookieService.RemoveTokenCookies(Response.Cookies);
        return Ok(new { message = "Logged out succesfully" });
    }
}
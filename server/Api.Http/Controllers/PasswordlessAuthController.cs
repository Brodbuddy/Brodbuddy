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

    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService,
        IDeviceDetectionService deviceDetectionService)
    {
        _authService = passwordlessAuthService;
        _deviceDetectionService = deviceDetectionService;
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

            return Ok(new { accessToken, refreshToken });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid verification code" });
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(new { accessToken, refreshToken });
        }
        catch (Exception)
        {
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }
}
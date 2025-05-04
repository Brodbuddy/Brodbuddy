using Api.Http.Models.Dto.Request;
using Api.Http.Models.Dto.Response;
using Api.Http.Utils;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[Route("api/passwordless-auth")]
[ApiController]
public class PasswordlessAuthController : ControllerBase
{
    private readonly IPasswordlessAuthService _authService;


    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService)
    {
        _authService = passwordlessAuthService;
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
    public async Task<ActionResult<LoginVerificationResponse>> VerifyCode([FromBody] LoginVerificationRequest request)
    {
        var browser = UserAgentUtils.GetBrowser(HttpContext);
        var os = UserAgentUtils.GetOperatingSystem(HttpContext);

        var (accessToken, refreshToken) = await _authService.CompleteLoginAsync(
            request.Email,
            request.Code,
            browser,
            os);

        return new LoginVerificationResponse(accessToken, refreshToken);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(request.RefreshToken);
        return new RefreshTokenResponse(accessToken, refreshToken);
    }
}
using Api.Http.Extensions;
using Api.Http.Models;
using Application;
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
    private readonly IJwtService _jwtService;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AppOptions> _appOptions;
    private const string RefreshTokenCookieName = "refreshToken";

    public PasswordlessAuthController(
        IPasswordlessAuthService passwordlessAuthService, 
        IDeviceDetectionService deviceDetectionService,
        IJwtService jwtService,
        TimeProvider timeProvider,
        IOptions<AppOptions> appOptions)
    {
        _authService = passwordlessAuthService;
        _deviceDetectionService = deviceDetectionService;
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
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken()
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
           
        Console.WriteLine("i refreshtoken");
        var (accessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken!);
            
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
    [Authorize(Roles = "user")]
    public async Task<UserInfoResponse> UserInfo()
    {
        var userInfo = await _authService.UserInfoAsync(HttpContext.User.GetUserId());
        return new UserInfoResponse(Email: userInfo.email, IsAdmin: false);
    }
    
    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_appOptions.Value.Token.RefreshTokenLifeTimeDays)
    };
}
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Http.Controllers;




[ApiController]
[Route("api/[controller]")]
public class IdentityVerificationController : ControllerBase
{
    private readonly IIdentityVerificationService _identityVerificationService;
    private readonly ILogger<IdentityVerificationController> _logger;

    public IdentityVerificationController(
        IIdentityVerificationService identityVerificationService,
        ILogger<IdentityVerificationController> logger)
    {
        _identityVerificationService = identityVerificationService;
        _logger = logger;
    }

    [HttpPost("send-code")]
    public async Task<IActionResult> SendCode([FromBody] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required.");
        }

        var result = await _identityVerificationService.SendCodeAsync(email);
        if (result)
        {
            return Ok("Verification code sent successfully.");
        }

        return BadRequest("Failed to send verification code.");
    }
    
    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Code <= 0)
        {
            return BadRequest("Email and code are required.");
        }

        var (verified, userId) = await _identityVerificationService.TryVerifyCodeAsync(request.Email, request.Code);
        if (verified)
        {
            return Ok(new { UserId = userId });
        }

        return BadRequest("Invalid verification code.");
    }
    
    
    public class VerifyCodeRequest
    {
        public string Email { get; set; }
        public int Code { get; set; }
    }
    
    
    
}
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Api.Http.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestAuthController(IJwtService jwtService, ILogger<TestAuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult GetUser()
    {
        logger.LogInformation("ApiKey: {ApiKey}", "asdfhjlasdfjashrau123766723");
        return Ok(jwtService.Generate("kakao", "kakao@mælk.dk", "user"));
    }

    [Authorize(Roles = "user")]
    [HttpGet("/hej")]
    public IActionResult DeleteUser()
    {
        return Ok();
    }
}
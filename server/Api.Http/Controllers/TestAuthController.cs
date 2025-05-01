using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TestAuthController(IJwtService jwtService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult GetUser()
    {
        return Ok(jwtService.Generate("kakao", "kakao@mælk.dk", "user"));
    }

    [Authorize(Roles = "user")]
    [HttpGet("/hej")]
    public IActionResult DeleteUser()
    {
        return Ok();
    }
    
    
    }
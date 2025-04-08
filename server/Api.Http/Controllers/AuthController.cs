
using Application;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Http.Controllers;

public class AuthController : ControllerBase
{
    private readonly IRefreshTokenService _refreshTokenService;
    
    public AuthController(IRefreshTokenService refreshTokenService)
    {
        _refreshTokenService = refreshTokenService;
    }
    
    [HttpGet("generate-token")]
    public async Task<IActionResult> GenerateToken()
    {
       
        var token = await _refreshTokenService.GenerateAsync();
        
        return Ok(new 
        {
            RefreshToken = token
        });
    }
}
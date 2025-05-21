using Application.Interfaces.Auth;
using Application.Models;
using Application.Services;
using Application.Services.Auth;

namespace Infrastructure.Auth;

public class JwtAuthenticationService : IAuthenticationService
{
    private readonly IJwtService _jwtService;
    
    public JwtAuthenticationService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }
    
    public Task<AuthenticationResult> ValidateTokenAsync(string token)
    {
        return Task.FromResult(_jwtService.TryValidate(token, out var claims) 
            ? new AuthenticationResult(true, claims.Sub, [claims.Role])
            : new AuthenticationResult(false));
    }
}
using Application.Models;

namespace Application.Interfaces.Auth;

public interface IAuthenticationService
{
    Task<AuthenticationResult> ValidateTokenAsync(string token);
}
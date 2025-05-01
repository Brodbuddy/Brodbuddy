using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthenticationService = Application.Interfaces.Auth.IAuthenticationService;

namespace Api.Http.Auth;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
{
    private readonly IAuthenticationService _authenticationService;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthenticationService authenticationService) 
        : base(options, logger, encoder) 
    {
        _authenticationService = authenticationService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader)) return AuthenticateResult.NoResult();

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue["Bearer ".Length..].Trim();
        var authResult = await _authenticationService.ValidateTokenAsync(token);
        if (!authResult.IsAuthenticated) return AuthenticateResult.Fail("Invalid token");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authResult.UserId!)
        };

        foreach (var role in authResult.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
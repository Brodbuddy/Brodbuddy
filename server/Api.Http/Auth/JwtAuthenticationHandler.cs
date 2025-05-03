using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IAuthenticationService = Application.Interfaces.Auth.IAuthenticationService;

namespace Api.Http.Auth;

public class JwtAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICookieService _cookieService;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAuthenticationService authenticationService,
        ICookieService cookieService) 
        : base(options, logger, encoder) 
    {
        _authenticationService = authenticationService;
        _cookieService = cookieService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = headerValue["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            token = _cookieService.GetAccessTokenFromCookies(Request.Cookies);

            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.NoResult();
            }
        }

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
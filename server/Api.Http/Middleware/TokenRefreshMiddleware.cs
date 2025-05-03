using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces.Auth;
using Application.Services;
using Microsoft.AspNetCore.Http;

namespace Api.Http.Middleware;

public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeProvider _timeProvider;

    public TokenRefreshMiddleware(
        RequestDelegate next,
        TimeProvider timeProvider)
    {
        _next = next;
        _timeProvider = timeProvider;
    }

    public async Task InvokeAsync(HttpContext context, ICookieService cookieService, IPasswordlessAuthService authService)
    {
        var accessToken = cookieService.GetAccessTokenFromCookies(context.Request.Cookies);
        
        if (!string.IsNullOrEmpty(accessToken))
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(accessToken))
            {
                var token = handler.ReadJwtToken(accessToken);
                var expiry = token.ValidTo;
                var currentTime = _timeProvider.GetUtcNow().UtcDateTime;
                
                // Hvis token er ved at udløbe indenfor de kommende 5 min, bliver det refreshet.
                if (expiry < currentTime.AddMinutes(5))
                {
                    var refreshToken = cookieService.GetRefreshTokenFromCookies(context.Request.Cookies);
                    if (!string.IsNullOrEmpty(refreshToken))
                    {
                        try
                        {
                            var (newAccessToken, newRefreshToken) = await authService.RefreshTokenAsync(refreshToken);
                            
                            cookieService.SetAccessTokenCookie(context.Response.Cookies, newAccessToken);
                            cookieService.SetRefreshTokenCookie(context.Response.Cookies, newRefreshToken);
                        }
                        catch
                        {
                            // Vi ignorerer fejl under token-fornyelse og fortsætter anmodningen
                            // ASP.NET Core's Authentication Middleware vil undersøge det eksisterende token
                            // og håndtere autentificeringen korrekt
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}
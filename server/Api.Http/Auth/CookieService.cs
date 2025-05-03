using Application;
using Application.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Http.Auth;

public class CookieService : ICookieService
{
    private readonly AppOptions _options;
    private readonly TimeProvider _timeProvider;
    private const string AccessTokenCookieName = "accesstoken";
    private const string RefreshTokenCookieName = "refreshtoken";

    public CookieService(IOptions<AppOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public void SetAccessTokenCookie(IResponseCookies cookies, string token)
    {
        cookies.Append(AccessTokenCookieName, token, GetAccessTokenCookieOptions());
    }

    public void SetRefreshTokenCookie(IResponseCookies cookies, string token)
    {
        cookies.Append(RefreshTokenCookieName, token, GetRefreshTokenCookieOptions());
    }

    public string? GetAccessTokenFromCookies(IRequestCookieCollection cookies)
    {
        return cookies[AccessTokenCookieName];
    }

    public string? GetRefreshTokenFromCookies(IRequestCookieCollection cookies)
    {
        return cookies[RefreshTokenCookieName];
    }

    public void RemoveTokenCookies(IResponseCookies cookies)
    {
        cookies.Delete(AccessTokenCookieName, new CookieOptions {SameSite = SameSiteMode.Strict});
        cookies.Delete(RefreshTokenCookieName, new CookieOptions { SameSite = SameSiteMode.Strict});
    }

    private CookieOptions GetAccessTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_options.Token.AccessTokenLifeTimeMinutes)
    };

    private CookieOptions GetRefreshTokenCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = _timeProvider.GetUtcNow().UtcDateTime.AddDays(_options.Token.RefreshTokenLifeTimeDays)
    };
}
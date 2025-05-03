using Microsoft.AspNetCore.Http;

namespace Application.Interfaces.Auth;

public interface ICookieService
{
    void SetAccessTokenCookie(IResponseCookies cookies, string token);
    void SetRefreshTokenCookie(IResponseCookies cookies, string token);
    string? GetAccessTokenFromCookies(IRequestCookieCollection cookies);
    string? GetRefreshTokenFromCookies(IRequestCookieCollection cookies);
    void RemoveTokenCookies(IResponseCookies cookies);
}
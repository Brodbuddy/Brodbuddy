using System.Security.Claims;

namespace Api.Http.Extensions;

public static class ClaimExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentException("User ID claim not found");

        return Guid.Parse(userIdClaim);
    }
}
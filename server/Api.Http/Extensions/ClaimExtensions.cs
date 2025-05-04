using System.Security.Claims;
using System.Text;

namespace Api.Http.Extensions;

public static class ClaimExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        // Debug the ClaimsPrincipal
        DumpClaimsPrincipal(principal);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new ArgumentException("User ID claim not found");

        return Guid.Parse(userIdClaim);
    }

    // Helper method to properly debug a ClaimsPrincipal
    private static void DumpClaimsPrincipal(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            Console.WriteLine("ClaimsPrincipal is null");
            return;
        }

        Console.WriteLine($"ClaimsPrincipal Identity: {principal.Identity?.Name ?? "null"}, Authenticated: {principal.Identity?.IsAuthenticated}");
        
        Console.WriteLine("Claims:");
        foreach (var claim in principal.Claims)
        {
            Console.WriteLine($"  Type: {claim.Type}, Value: {claim.Value}");
        }

        Console.WriteLine($"Total claims count: {principal.Claims.Count()}");
        
        // Check for specific claims
        Console.WriteLine($"Has NameIdentifier claim: {principal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier)}");
        Console.WriteLine($"Identity type: {principal.Identity?.GetType().FullName ?? "null"}");
    }
}
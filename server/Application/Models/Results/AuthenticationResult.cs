namespace Application.Models;

public class AuthenticationResult
{
    public bool IsAuthenticated { get; }
    public string? UserId { get; }
    public IReadOnlyList<string> Roles { get; }

    public AuthenticationResult(bool isAuthenticated, string? userId = null, IEnumerable<string>? roles = null)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        Roles = roles?.ToArray() ?? [];
    }

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasAnyRole(IEnumerable<string> requiredRoles)
    {
        var roleArray = requiredRoles as string[] ?? requiredRoles.ToArray();
        return roleArray.Length == 0 || roleArray.Any(HasRole);
    }
}
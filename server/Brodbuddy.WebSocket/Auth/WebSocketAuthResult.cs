namespace Brodbuddy.WebSocket.Auth;

public class WebSocketAuthResult(bool isAuthenticated, string? userId = null, IEnumerable<string>? roles = null)
{
    public bool IsAuthenticated { get; } = isAuthenticated;
    public string? UserId { get; } = userId;
    public IReadOnlyList<string> Roles { get; } = roles?.ToArray() ?? [];

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasAnyRole(IEnumerable<string> requiredRoles)
    {
        var enumerable = requiredRoles as string[] ?? requiredRoles.ToArray();
        return enumerable.Length == 0 || enumerable.Any(HasRole);
    }
}
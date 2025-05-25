namespace Brodbuddy.WebSocket.Auth;

[AttributeUsage(AttributeTargets.Class)]
public class AuthorizeAttribute : Attribute
{
    public string? Roles { get; set; }

    internal string[] GetRolesAsArray()
    {
        return string.IsNullOrWhiteSpace(Roles) 
            ? [] 
            : Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
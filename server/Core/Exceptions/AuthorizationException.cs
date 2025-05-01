namespace Core.Exceptions;

public sealed class AuthorizationException : Exception
{
    public string? RequiredPermission { get; }

    public AuthorizationException(string message)
        : base(message)
    {
        RequiredPermission = null;
    }

    public AuthorizationException(string message, string requiredPermission)
        : base(message)
    {
        RequiredPermission = requiredPermission;
    }

    public AuthorizationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        RequiredPermission = null;
    }

    public AuthorizationException(string message, string requiredPermission, Exception? innerException)
        : base(message, innerException)
    {
        RequiredPermission = requiredPermission;
    }
}
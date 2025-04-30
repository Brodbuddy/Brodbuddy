namespace Core.Exceptions;


public sealed class AuthenticationException : Exception
{
    public string? FailureReason { get; }

    public AuthenticationException(string message)
        : base(message)
    {
        FailureReason = null;
    }

    public AuthenticationException(string message, string failureReason)
        : base(message)
    {
        FailureReason = failureReason;
    }

    public AuthenticationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        FailureReason = null;
    }

    public AuthenticationException(string message, string failureReason, Exception? innerException)
        : base(message, innerException)
    {
        FailureReason = failureReason;
    }
}
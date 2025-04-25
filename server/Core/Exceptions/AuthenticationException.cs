namespace Core.Exceptions;



/// <summary>
/// Authentication fails.
/// Information about the reason for the authentication failure.
/// </summary>
public class AuthenticationException : Exception
{
    public string FailureReason { get; }

    public AuthenticationException(string message, string failureReason)
        : base(message)
    {
        FailureReason = failureReason;
    }

    public AuthenticationException(string message, string failureReason, Exception? innerException)
        : base(message, innerException)
    {
        FailureReason = failureReason;
    }
}
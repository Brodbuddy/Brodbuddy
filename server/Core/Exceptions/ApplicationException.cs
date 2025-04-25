namespace Core.Exceptions;

/// <summary>
/// Base exception class for all application-specific exceptions.
/// Provides common functionality and makes it easy to catch all application exceptions.
/// </summary>
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
    
    protected ApplicationException(string message, Exception? innerException) 
        : base(message, innerException) { }
}
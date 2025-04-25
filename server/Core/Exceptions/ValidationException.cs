namespace Core.Exceptions;

/// <summary>
/// Handles validation errors for input or data.
/// Can contain a collection of errors for different fields.
/// Used before data is saved to the database.
/// </summary>
public sealed class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }
    
    public ValidationException(string message, IDictionary<string, string[]> errors, Exception? innerException = null)
        : base(message, innerException)
    {
        Errors = errors;
    }
}
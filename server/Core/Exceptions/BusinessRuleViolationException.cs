namespace Core.Exceptions;

/// <summary>
/// Thrown when a business rule is violated.
/// Contains information about which rule was broken and why.
/// Useful for domain-driven design.
/// </summary>
public sealed class BusinessRuleViolationException : ApplicationException
{
    public string RuleName { get; }

    public BusinessRuleViolationException(string message, string ruleName) 
        : base(message)
    {
        RuleName = ruleName;
    }
    
    public BusinessRuleViolationException(string message, string ruleName, Exception? innerException) 
        : base(message, innerException)
    {
        RuleName = ruleName;
    }
}
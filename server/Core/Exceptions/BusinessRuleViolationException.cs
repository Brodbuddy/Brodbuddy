namespace Core.Exceptions;

public sealed class BusinessRuleViolationException : Exception
{
    public string? RuleName { get; }

    public BusinessRuleViolationException(string message)
        : base(message)
    {
        RuleName = null;
    }

    public BusinessRuleViolationException(string message, string ruleName)
        : base(message)
    {
        RuleName = ruleName;
    }

    public BusinessRuleViolationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        RuleName = null;
    }

    public BusinessRuleViolationException(string message, string ruleName, Exception? innerException)
        : base(message, innerException)
    {
        RuleName = ruleName;
    }
}
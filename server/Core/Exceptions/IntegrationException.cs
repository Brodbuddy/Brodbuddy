namespace Core.Exceptions;

/// <summary>
/// Handles errors in communication with external systems.
/// Can contain information about which system failed and why.
/// Useful in microservices or systems with many integrations.
/// </summary>
public sealed class IntegrationException : ApplicationException
{
    public string SystemName { get; }

    public IntegrationException(string message, string systemName, Exception? innerException = null)
        : base(message, innerException)
    {
        SystemName = systemName;
    }
}
namespace Core.Exceptions;

/// <summary>
/// Thrown when a configuration value is missing or invalid.
/// </summary>
public sealed class ConfigurationException : ApplicationException
{
    public string ConfigKey { get; }

    public ConfigurationException(string configKey, string? message = null)
        : base(message ?? $"Configuration error for key: {configKey}")
    {
        ConfigKey = configKey;
    }
    
    public ConfigurationException(string configKey, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ConfigKey = configKey;
    }
}
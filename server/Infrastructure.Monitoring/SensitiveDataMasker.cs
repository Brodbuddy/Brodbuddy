namespace Infrastructure.Monitoring;

public static class SensitiveDataMasker
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "apiKey",
        "connectionString",
        "credential",
    };

    public static object MaskSensitiveProperties(object? value)
    {
        if (value == null) return null!;

        if (value.GetType().IsPrimitive || value is string) return value;

        var properties = value.GetType().GetProperties();
        var maskNeeded = properties.Where(prop => prop is { CanWrite: true, CanRead: true })
                                        .Any(prop => SensitivePropertyNames.Contains(prop.Name));

        if (!maskNeeded) return value;

        var masked = Activator.CreateInstance(value.GetType());

        foreach (var prop in properties)
        {
            if (!prop.CanWrite || !prop.CanRead) continue;

            var propValue = prop.GetValue(value);
            prop.SetValue(masked, SensitivePropertyNames.Contains(prop.Name) ? "***MASKED***" : propValue);
        }

        return masked!;
    }
}
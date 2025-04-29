using System.Globalization;

namespace Infrastructure.Monitoring;

public static class SerilogConstants
{
    public const string ProductionOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
    public const string Application = "Application";
    public static readonly IFormatProvider DefaultFormatProvider = CultureInfo.InvariantCulture;
}
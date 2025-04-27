using Serilog;

namespace Infrastructure.Monitoring;

public static class LoggerFactory
{
    public static ILogger CreateBootstrapLogger()
    {
        return new LoggerConfiguration().MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: SerilogConstants.DefaultFormatProvider)
            .CreateBootstrapLogger();
    }
}
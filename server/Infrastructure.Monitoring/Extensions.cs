using Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace Infrastructure.Monitoring;

public static class MonitoringExtensions
{
    public static IHostBuilder AddMonitoringInfrastructure(this IHostBuilder builder, string? applicationName = null)
    {
        builder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var options = services.GetRequiredService<IOptions<AppOptions>>().Value;

            ConfigureBasicLogging(loggerConfiguration, context, applicationName);
            ConfigureConsoleLogging(loggerConfiguration, context.HostingEnvironment);
            ConfigureSeqLogging(loggerConfiguration, options.Seq);
        });

        return builder;
    }

    private static void ConfigureBasicLogging(LoggerConfiguration loggerConfiguration, HostBuilderContext context,
        string? applicationName)
    {
        loggerConfiguration
            .Destructure.ByTransforming<object>(SensitiveDataMasker.MaskSensitiveProperties)
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithClientIp()
            .Enrich.WithExceptionDetails();

        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            loggerConfiguration.Enrich.WithProperty(SerilogConstants.Application, applicationName);
        }
    }

    private static void ConfigureConsoleLogging(LoggerConfiguration loggerConfiguration, IHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: SerilogConstants.ProductionOutputTemplate,
                formatProvider: SerilogConstants.DefaultFormatProvider
            );
        }
        else
        {
            loggerConfiguration.WriteTo.Console(
                formatProvider: SerilogConstants.DefaultFormatProvider
            );
        }
    }

    private static void ConfigureSeqLogging(LoggerConfiguration loggerConfiguration, SeqOptions seqOptions)
    {
        loggerConfiguration.WriteTo.Seq(
            serverUrl: seqOptions.ServerUrl,
            apiKey: string.IsNullOrEmpty(seqOptions.ApiKey) ? null : seqOptions.ApiKey,
            formatProvider: SerilogConstants.DefaultFormatProvider
        );
    }

    public static IApplicationBuilder UseMonitoringInfrastructure(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIpAddress",
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);

                var correlationId = httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                                    httpContext.TraceIdentifier;
                diagnosticContext.Set("CorrelationId", correlationId);
            };

            options.GetLevel = (httpContext, _, ex) =>
            {
                if (ex != null || httpContext.Response.StatusCode > 499) return LogEventLevel.Error;
                return httpContext.Response.StatusCode > 399 ? LogEventLevel.Warning : LogEventLevel.Information;
            };
        });
        return app;
    }
}
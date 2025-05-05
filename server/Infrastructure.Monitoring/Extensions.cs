using System.Diagnostics;
using Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using Serilog.Exceptions;

namespace Infrastructure.Monitoring;

public static class MonitoringExtensions
{
    private const string ServiceVersion = "1.0.0";

    public static IServiceCollection AddMonitoringInfrastructure(this IServiceCollection services, string serviceName,
        IHostEnvironment environment)
    {
        services.AddSingleton(new ActivitySource(serviceName));

        services.Configure<ZipkinExporterOptions>(_ =>
        {
            // Tom konfiguration som bliver konfigureret senere med IConfiguration
        });

        services.AddSingleton<IConfigureOptions<ZipkinExporterOptions>>(serviceProvider =>
        {
            return new ConfigureNamedOptions<ZipkinExporterOptions>(Options.DefaultName, (options) =>
            {
                var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;

                if (!string.IsNullOrWhiteSpace(appOptions.Zipkin.Endpoint) && Uri.TryCreate(appOptions.Zipkin.Endpoint, UriKind.Absolute, out var zipkinUri))
                {
                    options.Endpoint = zipkinUri;
                }
                else
                {
                    Log.Warning("OpenTelemetry: Zipkin endpoint invalid or not configured in AppOptions. Zipkin exporter disabled.");
                }
            });
        });

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: serviceName, serviceVersion: ServiceVersion, serviceInstanceId: Environment.MachineName))
            .WithTracing(builder =>
            {
                builder.SetSampler(sp =>
                    {
                        var appOptions = sp.GetRequiredService<IOptions<AppOptions>>().Value;
                        return environment.IsDevelopment()
                            ? new AlwaysOnSampler()
                            : new ParentBasedSampler(new TraceIdRatioBasedSampler(appOptions.Zipkin.SamplingRate));
                    })
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText =
                            environment.IsDevelopment(); // Log kun SQL i development for debug
                    });

                // builder.AddConsoleExporter(); // Til lokal udvikling 

                builder.AddZipkinExporter();
            });
        return services;
    }

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
        loggerConfiguration.ReadFrom.Configuration(context.Configuration)
            .Enrich.WithSensitiveDataMasking(options =>
            {
                // Indeholder som standard: Email, IBAN og CreditCard
                options.MaskProperties.Add("apikey");
                options.MaskProperties.Add("password");
                options.MaskProperties.Add("secret");
                options.MaskProperties.Add("token");
                options.MaskProperties.Add("connectionString");
                options.MaskProperties.Add("credential");
            })
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

    public static IApplicationBuilder ConfigureMonitoringInfrastructure(this IApplicationBuilder app)
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
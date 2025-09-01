using Itenium.Forge.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using System.Reflection;

namespace Itenium.Forge.Logging;

public static class LoggingExtensions
{
    /// <summary>
    /// Adds logging to Console, Rolling File and (optionally) Grafana Loki.
    /// If your appsettings does not contain a Serilog section, it will default to serilog.settings.json.
    /// Attempts to resolve ForgeSettings and enriches the logger with them.
    /// </summary>
    public static void AddForgeLogging(this WebApplicationBuilder builder)
    {
        var loggingConfig = builder.Configuration.GetSection("ForgeConfiguration:Logging").Get<LoggingConfiguration>();
        var forgeSettings = builder.Configuration.GetSection("Forge").Get<ForgeSettings>();

        if (builder.Configuration.GetSection("Serilog").Exists())
        {
            if (!string.IsNullOrWhiteSpace(loggingConfig?.FilePath))
                throw new Exception("Cannot have a custom Serilog appSettings and set the ForgeConfiguration:Logging:FilePath");
        }
        else
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string embeddedResourceName = "Itenium.Forge.Logging.serilog.settings.json";
            using var defaultSerilogSettings = assembly.GetManifestResourceStream(embeddedResourceName)!;
            var actualConfiguration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonStream(defaultSerilogSettings)
                .Build();

            if (!string.IsNullOrWhiteSpace(loggingConfig?.FilePath))
            {
                string logPath = loggingConfig.FilePath;
                if (string.IsNullOrWhiteSpace(Path.GetExtension(logPath)))
                {
                    logPath = Path.Combine(logPath, "log-.txt");
                }
                actualConfiguration["Serilog:WriteTo:1:Args:path"] = logPath;
            }

            builder.Configuration.AddConfiguration(actualConfiguration);
        }

        builder.Services.AddSerilog((services, lc) =>
        {
            lc.ReadFrom.Configuration(builder.Configuration);

            // If we need injected services for something
            // lc.ReadFrom.Services(services);

            lc.Enrich.FromLogContext();
            lc.Enrich.WithClientIp();
            lc.Enrich.WithMachineName();
            lc.Enrich.WithThreadId();

            lc.Enrich.WithRequestHeader("x-correlation-id", "CorrelationId");
            // TODO: logging enrichment: CorrelationId // Activity.Current?.TraceId.ToString(); ?
            // TODO: logging enrichment: UserId/Name

            if (forgeSettings != null)
            {
                lc.Enrich.WithProperty("Environment", forgeSettings.Environment);
                lc.Enrich.WithProperty("Application", forgeSettings.Application);
                lc.Enrich.WithProperty("service_name", forgeSettings.ServiceName);
                if (!string.IsNullOrWhiteSpace(forgeSettings.TeamName))
                {
                    lc.Enrich.WithProperty("TeamName", forgeSettings.TeamName);
                }
                if (!string.IsNullOrWhiteSpace(forgeSettings.Tenant))
                {
                    lc.Enrich.WithProperty("Tenant", forgeSettings.Tenant);
                }
            }
            else
            {
                lc.Enrich.WithProperty("Environment", builder.Environment.EnvironmentName);
                lc.Enrich.WithProperty("service_name", builder.Environment.ApplicationName);
            }

            if (!string.IsNullOrWhiteSpace(loggingConfig?.LokiUrl))
            {
                lc.WriteTo.GrafanaLoki(
                    loggingConfig.LokiUrl,
                    [
                        // new LokiLabel() { Key = "service_name", Value = forgeSettings?.Application ?? "service-name" },
                    ],
                    [
                        "level",
                        "Environment",
                        "Application",
                        "MachineName",
                        "StatusCode",
                        "service_name"
                    ]
                );
            }
        });

        if (forgeSettings != null)
        {
            Log.Logger.Information("Starting web application with {@Settings}", forgeSettings);
        }
        else
        {
            Log.Logger.Error("Starting web application {Application} without ForgeSettings for {Environment}", builder.Environment.ApplicationName, builder.Environment.EnvironmentName);
        }


        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


        // TODO: This should be in Forge.Telemetry
        // TODO: need: OpenTelemetry.Exporter.Prometheus.AspNetCore
        // TODO: and also:
        //<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
        //<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
        //<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
        //<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
        //using OpenTelemetry.Metrics;
        //using OpenTelemetry.Resources;
        //builder.Services
        //    .AddOpenTelemetry()
        //    .ConfigureResource(r =>
        //    {
        //        r.AddService(builder.Environment.ApplicationName);
        //    })
        //    .WithMetrics(metrics =>
        //    {
        //        metrics.AddAspNetCoreInstrumentation();
        //        metrics.AddHttpClientInstrumentation();
        //        metrics.AddRuntimeInstrumentation();
        //        metrics.AddPrometheusExporter();
        //    });
        //    .WithTracing(tracing =>
        //    {
        //        tracing.AddAspNetCoreInstrumentation();
        //    });
    }

    /// <summary>
    /// Setup our custom request logger <see cref="RequestLoggingMiddleware"/>.
    /// </summary>
    public static void UseForgeLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Startup>>();
        var forgeSettings = app.Services.GetService<ForgeSettings>();

        if (forgeSettings != null)
        {
            logger.LogInformation("Built web application with {@Settings}", forgeSettings);
        }
        else
        {
            logger.LogError("Built web application {Application} without ForgeSettings for {Environment}", app.Environment.ApplicationName, app.Environment.EnvironmentName);
        }

        // TODO: This should be in Forge.Telemetry
        // app.UseOpenTelemetryPrometheusScrapingEndpoint();

        // Alternatively: app.UseSerilogRequestLogging();
        app.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Creates the Serilog bootstrap logger (console + startup.txt file)
    /// </summary>
    public static Serilog.ILogger CreateBootstrapLogger()
    {
        var logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/startup-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message} {Properties}{NewLine}{Exception}"
            )
            .CreateBootstrapLogger();

        logger.Information("Starting web application");

        return logger;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class Startup { }
}

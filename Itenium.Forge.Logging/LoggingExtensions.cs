using Itenium.Forge.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Grafana.Loki;
using System.Diagnostics;
using System.Reflection;

namespace Itenium.Forge.Logging;

public static class LoggingExtensions
{
    /// <summary>
    /// Adds Serilog (Console, Rolling File, optional Loki sink).
    /// For OpenTelemetry tracing and metrics call <c>AddForgeTelemetry()</c> from
    /// the <c>Itenium.Forge.Telemetry</c> package.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configureMasking">
    /// Optional delegate to configure which fields are masked in request logs.
    /// When <c>null</c>, the <see cref="FieldMaskingOptions.DefaultFields"/> are used.
    /// </param>
    public static void AddForgeLogging(this WebApplicationBuilder builder, Action<FieldMaskingOptions>? configureMasking = null)
    {
        var loggingConfig = builder.Configuration.GetSection("ForgeConfiguration:Logging").Get<LoggingConfiguration>();
        var forgeSettings = builder.Configuration.GetSection("Forge").Get<ForgeSettings>();

        var maskingOptions = new FieldMaskingOptions();
        configureMasking?.Invoke(maskingOptions);

        ConfigureSerilog(builder, loggingConfig, forgeSettings, maskingOptions);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddTransient<TraceparentHandler>();
        builder.Services.ConfigureHttpClientDefaults(b => b.AddHttpMessageHandler<TraceparentHandler>());

        builder.Services.AddSingleton(maskingOptions);
    }

    /// <summary>
    /// Registers the Forge middleware pipeline: <see cref="CorrelationIdMiddleware"/> then request logging.
    /// </summary>
    public static void UseForgeLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Startup>>();
        var forgeSettings = app.Services.GetService<ForgeSettings>();

        if (forgeSettings != null)
            logger.LogInformation("Built web application with {@Settings}", forgeSettings);
        else
            logger.LogError("Built web application {Application} without ForgeSettings for {Environment}", app.Environment.ApplicationName, app.Environment.EnvironmentName);

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
    }

    /// <summary>
    /// Creates the Serilog bootstrap logger (console + startup.txt file).
    /// Call this before <c>WebApplication.CreateBuilder</c> so startup errors are captured.
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

    // -------------------------------------------------------------------------

    private static void ConfigureSerilog(
        WebApplicationBuilder builder,
        LoggingConfiguration? loggingConfig,
        ForgeSettings? forgeSettings,
        FieldMaskingOptions maskingOptions)
    {
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
                    logPath = Path.Combine(logPath, "log-.txt");

                actualConfiguration["Serilog:WriteTo:1:Args:path"] = logPath;
            }

            builder.Configuration.AddConfiguration(actualConfiguration);
        }

        builder.Services.AddSerilog((services, lc) =>
        {
            lc.ReadFrom.Configuration(builder.Configuration);
            lc.Enrich.FromLogContext();
            lc.Enrich.WithClientIp();
            lc.Enrich.WithMachineName();
            lc.Enrich.WithThreadId();
            lc.Enrich.With<ActivityEnricher>();
            lc.Destructure.With(new ObjectMaskerDestructurePolicy(maskingOptions, services));

            // TODO: logging enrichment: UserId/Name

            if (forgeSettings != null)
            {
                lc.Enrich.WithProperty("Environment", forgeSettings.Environment);
                lc.Enrich.WithProperty("Application", forgeSettings.Application);
                lc.Enrich.WithProperty("service_name", forgeSettings.ServiceName);
                if (!string.IsNullOrWhiteSpace(forgeSettings.TeamName))
                    lc.Enrich.WithProperty("TeamName", forgeSettings.TeamName);
                if (!string.IsNullOrWhiteSpace(forgeSettings.Tenant))
                    lc.Enrich.WithProperty("Tenant", forgeSettings.Tenant);
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
                    [],
                    ["level", "Environment", "Application", "MachineName", "StatusCode", "service_name"]
                );
            }
        });

        if (!string.IsNullOrWhiteSpace(loggingConfig?.LokiUrl))
        {
            builder.Services.AddHttpClient("LokiHealthCheck");
            var lokiUrl = loggingConfig.LokiUrl;
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    "loki",
                    sp => new LokiHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), lokiUrl),
                    HealthStatus.Degraded,
                    ["ready"]
                ));
        }

        if (forgeSettings != null)
            Log.Logger.Information("Starting web application with {@Settings}", forgeSettings);
        else
            Log.Logger.Error("Starting web application {Application} without ForgeSettings for {Environment}", builder.Environment.ApplicationName, builder.Environment.EnvironmentName);
    }

    /// <summary>Enriches every Serilog log event with the OTel TraceId and SpanId from the current Activity.</summary>
    private class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity is null) return;

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class Startup { }
}

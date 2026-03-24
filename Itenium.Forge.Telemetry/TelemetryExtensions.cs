using Itenium.Forge.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Itenium.Forge.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing and optional metrics/OTLP export.
    /// Tracing is always active so <c>Activity.Current</c> and the W3C <c>traceparent</c>
    /// header are populated per request without any additional configuration.
    /// OTLP export and Prometheus metrics are opt-in via <c>ForgeConfiguration:Telemetry</c>.
    /// </summary>
    public static void AddForgeTelemetry(this WebApplicationBuilder builder)
    {
        var telemetryConfig = builder.Configuration
            .GetSection("ForgeConfiguration:Telemetry")
            .Get<TelemetryConfiguration>();

        var forgeSettings = builder.Configuration
            .GetSection("Forge")
            .Get<ForgeSettings>();

        var serviceName = forgeSettings?.ServiceName ?? builder.Environment.ApplicationName;
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";

        var otelBuilder = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName, serviceVersion: serviceVersion);
                if (forgeSettings != null)
                {
                    r.AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = forgeSettings.Environment,
                        ["service.team"] = forgeSettings.TeamName ?? "",
                        ["service.tenant"] = forgeSettings.Tenant ?? ""
                    });
                }
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(telemetryConfig?.OtlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(telemetryConfig.OtlpEndpoint));
            });

        if (telemetryConfig?.MetricsEnabled == true)
        {
            otelBuilder.WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddPrometheusExporter();
            });
        }

        if (!string.IsNullOrWhiteSpace(telemetryConfig?.OtlpEndpoint))
        {
            builder.Services.AddHttpClient("OtlpHealthCheck");
            var otlpEndpoint = telemetryConfig.OtlpEndpoint;
            builder.Services.AddHealthChecks()
                .Add(new HealthCheckRegistration(
                    "otlp",
                    sp => new OtlpHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), otlpEndpoint),
                    HealthStatus.Degraded,
                    ["ready"]
                ));
        }
    }

    /// <summary>
    /// Exposes the <c>/metrics</c> endpoint for Prometheus scraping when metrics are enabled.
    /// Call this after <c>UseForgeLogging()</c> but before <c>UseForgeControllers()</c>.
    /// </summary>
    public static void UseForgeTelemetry(this WebApplication app)
    {
        var telemetryConfig = app.Configuration
            .GetSection("ForgeConfiguration:Telemetry")
            .Get<TelemetryConfiguration>();

        if (telemetryConfig?.MetricsEnabled == true)
        {
            app.UseOpenTelemetryPrometheusScrapingEndpoint();
        }
    }
}

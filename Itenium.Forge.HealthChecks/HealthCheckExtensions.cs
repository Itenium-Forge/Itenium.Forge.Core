using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers health check services with a basic "self" check.
    /// Additional checks can be added by the application using the standard
    /// <c>builder.Services.AddHealthChecks().Add*()</c> methods with tags "live" and/or "ready".
    /// </summary>
    /// <param name="builder">The WebApp builder</param>
    public static IHealthChecksBuilder AddForgeHealthChecks(this WebApplicationBuilder builder)
    {
        return builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);
    }

    /// <summary>
    /// Maps health check endpoints:
    /// <list type="bullet">
    /// <item><c>/health/live</c> - Liveness probe (fast, no dependency checks)</item>
    /// <item><c>/health/ready</c> - Readiness probe (includes dependency checks)</item>
    /// </list>
    /// Both endpoints include ForgeSettings metadata in the response.
    /// </summary>
    /// <param name="app">The WebApplication</param>
    public static void UseForgeHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = ForgeHealthCheckResponseWriter.WriteResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = ForgeHealthCheckResponseWriter.WriteResponse
        });
    }
}

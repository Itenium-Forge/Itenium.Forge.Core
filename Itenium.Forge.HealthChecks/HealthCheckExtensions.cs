using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HealthChecks;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Registers health check services /health/live and /health/ready
    /// </summary>
    public static IHealthChecksBuilder AddForgeHealthChecks(this WebApplicationBuilder builder)
    {
        return builder.Services
            .AddHealthChecks()
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

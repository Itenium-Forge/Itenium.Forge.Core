using Itenium.Forge.ExampleCoachingService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HttpClients.Tests;

/// <summary>
/// Factory that starts <see cref="Itenium.Forge.ExampleCoachingService"/> in-process for integration tests.
/// External health checks (loki, otlp) are removed so tests don't depend on running infrastructure.
/// </summary>
public class ExampleCoachingServiceFactory : WebApplicationFactory<ExampleCoachingSettings>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            RemoveHealthCheck(services, "loki");
            RemoveHealthCheck(services, "otlp");
        });
    }

    private static void RemoveHealthCheck(IServiceCollection services, string name)
    {
        services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            var registration = options.Registrations.FirstOrDefault(r => r.Name == name);
            if (registration != null)
                options.Registrations.Remove(registration);
        });
    }
}

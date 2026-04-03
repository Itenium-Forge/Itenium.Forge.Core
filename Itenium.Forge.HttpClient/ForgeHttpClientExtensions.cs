using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Refit;

namespace Itenium.Forge.HttpClients;

public static class ForgeHttpClientExtensions
{
    /// <summary>
    /// Registers <typeparamref name="T"/> as a Refit client backed by <c>ForgeConfiguration:HttpClients:{name}</c>,
    /// with traceparent propagation and a <c>ready</c>-tagged health check named <c>http-{name}</c>.
    /// </summary>
    public static IHttpClientBuilder AddForgeHttpClient<T>(this WebApplicationBuilder builder, string name)
        where T : class
    {
        RegisterOptionsOnce(builder.Services);

        var healthCheckName = $"http-{name}";

        builder.Services.AddHealthChecks()
            .Add(new HealthCheckRegistration(
                healthCheckName,
                sp => new HttpClientHealthCheck(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    name,
                    ResolveEntry(sp, name).ResolvedHealthCheckUrl),
                failureStatus: null,
                tags: ["ready"]
            ));

        return builder.Services
            .AddRefitClient<T>(settings: null, httpClientName: name)
            .ConfigureHttpClient((sp, client) =>
            {
                var entry = ResolveEntry(sp, name);
                client.BaseAddress = new Uri(entry.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(entry.TimeoutSeconds);
            });
    }

    private static HttpClientEntryOptions ResolveEntry(IServiceProvider sp, string name)
    {
        var options = sp.GetRequiredService<IOptions<HttpClientsOptions>>().Value;
        if (!options.HttpClients.TryGetValue(name, out var entry))
            throw new InvalidOperationException(
                $"No HttpClients entry found for '{name}' in ForgeConfiguration:HttpClients.");
        return entry;
    }

    // Marker to ensure options are only configured once per DI container
    private sealed class HttpClientsOptionsRegistered;

    private static void RegisterOptionsOnce(IServiceCollection services)
    {
        if (services.Any(static d => d.ServiceType == typeof(HttpClientsOptionsRegistered)))
            return;

        services.TryAddSingleton<HttpClientsOptionsRegistered>();

        services
            .AddOptions<HttpClientsOptions>()
            .BindConfiguration("ForgeConfiguration")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HttpClients.Tests;

[TestFixture]
public class HttpClientHealthCheckTests
{
    [Test]
    public async Task HealthCheck_WhenDownstreamResponds200_ReturnsHealthy()
    {
        using var factory = new ExampleCoachingServiceFactory();
        var check = new HttpClientHealthCheck(
            new FixedHttpClientFactory(factory.CreateClient()),
            "ExampleCoachingService",
            "/health/ready");

        var result = await RunCheckAsync(check);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task HealthCheck_WhenDownstreamUnreachable_ReturnsUnhealthy()
    {
        var httpClient = new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost:19999") };
        var check = new HttpClientHealthCheck(
            new FixedHttpClientFactory(httpClient),
            "ExampleCoachingService",
            "http://localhost:19999/health/ready");

        var result = await RunCheckAsync(check);

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
    }

    private static Task<HealthCheckResult> RunCheckAsync(IHealthCheck check) =>
        check.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("test", _ => check, null, null)
        });

    /// <summary>Wraps a pre-configured HttpClient for injection into the health check.</summary>
    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly System.Net.Http.HttpClient _client;
        public FixedHttpClientFactory(System.Net.Http.HttpClient client) => _client = client;
        public System.Net.Http.HttpClient CreateClient(string name) => _client;
    }
}

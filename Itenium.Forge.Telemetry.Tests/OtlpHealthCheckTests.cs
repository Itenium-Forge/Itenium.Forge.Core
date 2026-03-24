using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace Itenium.Forge.Telemetry.Tests;

[TestFixture]
public class OtlpHealthCheckTests
{
    private const string OtlpEndpoint = "http://localhost:4317";

    [Test]
    public async Task CheckHealthAsync_WhenEndpointResponds_ReturnsHealthy()
    {
        // Any HTTP response (including 4xx) means the collector is reachable
        var factory = FakeHttpClientFactory.Returning(HttpStatusCode.OK);
        var check = new OtlpHealthCheck(factory, OtlpEndpoint);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task CheckHealthAsync_WhenEndpointReturns4xx_ReturnsHealthy()
    {
        // 4xx still means the endpoint is up (the collector responded)
        var factory = FakeHttpClientFactory.Returning(HttpStatusCode.BadRequest);
        var check = new OtlpHealthCheck(factory, OtlpEndpoint);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task CheckHealthAsync_WhenConnectionRefused_ReturnsUnhealthy()
    {
        var factory = FakeHttpClientFactory.Throwing(new HttpRequestException("Connection refused"));
        var check = new OtlpHealthCheck(factory, OtlpEndpoint);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("unreachable"));
    }

    [Test]
    public async Task CheckHealthAsync_WhenConnectionRefused_IncludesExceptionMessage()
    {
        var factory = FakeHttpClientFactory.Throwing(new HttpRequestException("Connection refused"));
        var check = new OtlpHealthCheck(factory, OtlpEndpoint);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Exception, Is.Not.Null);
        Assert.That(result.Exception!.Message, Does.Contain("Connection refused"));
    }

    private static HealthCheckContext MakeContext() => new()
    {
        Registration = new HealthCheckRegistration("otlp", _ => null!, null, null)
    };
}

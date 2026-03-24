using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class LokiHealthCheckTests
{
    private const string LokiUrl = "http://loki:3100";

    [Test]
    public async Task CheckHealthAsync_WhenLokiReturns200_ReturnsHealthy()
    {
        var factory = FakeHttpClientFactory.Returning(HttpStatusCode.OK);
        var check = new LokiHealthCheck(factory, LokiUrl);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
    }

    [Test]
    public async Task CheckHealthAsync_WhenLokiReturnsServerError_ReturnsUnhealthy()
    {
        var factory = FakeHttpClientFactory.Returning(HttpStatusCode.InternalServerError);
        var check = new LokiHealthCheck(factory, LokiUrl);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("InternalServerError"));
    }

    [Test]
    public async Task CheckHealthAsync_WhenConnectionRefused_ReturnsUnhealthy()
    {
        var factory = FakeHttpClientFactory.Throwing(new HttpRequestException("Connection refused"));
        var check = new LokiHealthCheck(factory, LokiUrl);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain(LokiUrl));
    }

    [Test]
    public async Task CheckHealthAsync_WhenCancelled_ReturnsUnhealthy()
    {
        var factory = FakeHttpClientFactory.Throwing(new OperationCanceledException());
        var check = new LokiHealthCheck(factory, LokiUrl);

        var result = await check.CheckHealthAsync(MakeContext());

        Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        Assert.That(result.Description, Does.Contain("timed out"));
    }

    [Test]
    public async Task CheckHealthAsync_CallsReadyEndpoint()
    {
        Uri? requestedUri = null;
        var factory = FakeHttpClientFactory.Intercepting(
            HttpStatusCode.OK,
            uri => requestedUri = uri);
        var check = new LokiHealthCheck(factory, LokiUrl);

        await check.CheckHealthAsync(MakeContext());

        Assert.That(requestedUri?.ToString(), Is.EqualTo($"{LokiUrl}/ready"));
    }

    [Test]
    public async Task CheckHealthAsync_TrimsTrailingSlash()
    {
        Uri? requestedUri = null;
        var factory = FakeHttpClientFactory.Intercepting(
            HttpStatusCode.OK,
            uri => requestedUri = uri);
        var check = new LokiHealthCheck(factory, LokiUrl + "/");

        await check.CheckHealthAsync(MakeContext());

        Assert.That(requestedUri?.ToString(), Is.EqualTo($"{LokiUrl}/ready"));
    }

    private static HealthCheckContext MakeContext() => new()
    {
        Registration = new HealthCheckRegistration("loki", _ => null!, null, null)
    };
}

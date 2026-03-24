using System.Net;
using System.Text.Json;

namespace Itenium.Forge.ExampleApp.Tests;

/// <summary>
/// Verifies the full traceparent → TraceIdentifier → ProblemDetails chain.
/// OTel ASP.NET Core instrumentation creates an Activity per request, which
/// CorrelationIdMiddleware reads to set HttpContext.TraceIdentifier.
/// </summary>
[TestFixture]
public class CorrelationIdMiddlewareTests
{
    private ExampleAppFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ExampleAppFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Request_TraceId_AppearsInProblemDetails()
    {
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(problem.TryGetProperty("traceId", out var traceId), Is.True);
        Assert.That(traceId.GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Request_TraceId_IsOtelFormat()
    {
        // OTel trace IDs are 32 lowercase hex characters (128-bit), not a GUID
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        var traceId = problem.GetProperty("traceId").GetString()!;
        Assert.That(traceId, Has.Length.EqualTo(32));
        Assert.That(traceId, Does.Match("^[0-9a-f]{32}$"), "Expected 32 lowercase hex chars");
    }

    [Test]
    public async Task Requests_EachGetUniqueTraceId()
    {
        var response1 = await _client.GetAsync("/api/problem/bad-request");
        var response2 = await _client.GetAsync("/api/problem/bad-request");

        var id1 = JsonSerializer.Deserialize<JsonElement>(await response1.Content.ReadAsStringAsync())
            .GetProperty("traceId").GetString();
        var id2 = JsonSerializer.Deserialize<JsonElement>(await response2.Content.ReadAsStringAsync())
            .GetProperty("traceId").GetString();

        Assert.That(id1, Is.Not.EqualTo(id2));
    }
}

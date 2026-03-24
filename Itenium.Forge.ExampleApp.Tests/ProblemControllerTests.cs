using System.Net;
using System.Text.Json;

namespace Itenium.Forge.ExampleApp.Tests;

[TestFixture]
public class ProblemControllerTests
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
    public async Task BadRequest_ReturnsProblemDetails_With400()
    {
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(400));
        Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Unauthorized_ReturnsProblemDetails_With401()
    {
        var response = await _client.GetAsync("/api/problem/unauthorized");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(401));
        Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Forbidden_ReturnsProblemDetails_With403()
    {
        var response = await _client.GetAsync("/api/problem/forbidden");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(403));
        Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task NotFound_ReturnsProblemDetails_With404()
    {
        var response = await _client.GetAsync("/api/problem/not-found");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(404));
        Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Exception_ReturnsProblemDetails_With500()
    {
        var response = await _client.GetAsync("/api/problem/exception");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(500));
        Assert.That(problem.GetProperty("title").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task ValidationError_ReturnsProblemDetails_With400()
    {
        var response = await _client.GetAsync("/api/problem/validation");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(400));
        Assert.That(problem.TryGetProperty("errors", out _), Is.True);
    }

    [Test]
    public async Task NonExistentRoute_ReturnsProblemDetails_With404()
    {
        var response = await _client.GetAsync("/api/does-not-exist");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(404));
    }

    [Test]
    public async Task ProblemDetails_IncludesInstance()
    {
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(problem.GetProperty("instance").GetString(), Is.EqualTo("/api/problem/bad-request"));
    }

    [Test]
    public async Task ProblemDetails_IncludesTraceId()
    {
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(problem.TryGetProperty("traceId", out var traceId), Is.True);
        Assert.That(traceId.GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task ProblemDetails_TraceId_IsOtelFormat()
    {
        // OTel generates a 128-bit trace ID represented as 32 lowercase hex characters.
        // The middleware falls back to a GUID (32 hex, no dashes) when no Activity exists.
        // Either way the value must be a 32-char hex string.
        var response = await _client.GetAsync("/api/problem/bad-request");
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        var traceId = problem.GetProperty("traceId").GetString();
        Assert.That(traceId, Has.Length.EqualTo(32));
        Assert.That(traceId, Does.Match("^[0-9a-f]{32}$"));
    }

    [Test]
    public async Task ProblemDetails_TraceId_PropagatedFromTraceparentHeader()
    {
        // Send a W3C traceparent header — the OTel SDK will use its traceId segment
        // and the middleware will forward that to HttpContext.TraceIdentifier → ProblemDetails.
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/problem/bad-request");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await _client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<JsonElement>(content);

        var bodyTraceId = problem.GetProperty("traceId").GetString();
        Assert.That(bodyTraceId, Is.EqualTo(traceId));
    }
}

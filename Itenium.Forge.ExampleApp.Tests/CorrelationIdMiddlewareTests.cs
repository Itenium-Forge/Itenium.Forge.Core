using System.Net;

namespace Itenium.Forge.ExampleApp.Tests;

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
    public async Task Request_WithoutCorrelationId_ResponseContainsGeneratedHeader()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Headers.Contains("x-correlation-id"), Is.True);

        var correlationId = response.Headers.GetValues("x-correlation-id").Single();
        Assert.That(Guid.TryParse(correlationId, out _), Is.True, "Generated correlation ID should be a valid GUID");
    }

    [Test]
    public async Task Request_WithCorrelationId_ResponseEchoesSameHeader()
    {
        var sentId = "my-trace-abc-123";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("x-correlation-id", sentId);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var returnedId = response.Headers.GetValues("x-correlation-id").Single();
        Assert.That(returnedId, Is.EqualTo(sentId));
    }

    [Test]
    public async Task Requests_WithoutCorrelationId_EachGetUniqueId()
    {
        var response1 = await _client.GetAsync("/health/live");
        var response2 = await _client.GetAsync("/health/live");

        var id1 = response1.Headers.GetValues("x-correlation-id").Single();
        var id2 = response2.Headers.GetValues("x-correlation-id").Single();

        Assert.That(id1, Is.Not.EqualTo(id2));
    }
}

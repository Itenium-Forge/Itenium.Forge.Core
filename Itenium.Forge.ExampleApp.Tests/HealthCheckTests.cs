using System.Net;
using System.Text.Json;

namespace Itenium.Forge.ExampleApp.Tests;

[TestFixture]
public class HealthCheckTests
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

    #region Liveness Endpoint

    [Test]
    public async Task HealthLive_ReturnsJsonWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task HealthLive_IncludesForgeSettings()
    {
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        var settings = json.GetProperty("settings");
        Assert.That(settings.GetProperty("serviceName").GetString(), Is.EqualTo("Forge.ExampleApp"));
        Assert.That(settings.GetProperty("application").GetString(), Is.EqualTo("ExampleApp"));
        Assert.That(settings.GetProperty("teamName").GetString(), Is.EqualTo("Core"));
        Assert.That(settings.GetProperty("tenant").GetString(), Is.EqualTo("itenium"));
    }

    [Test]
    public async Task HealthLive_IncludesChecks()
    {
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(json.TryGetProperty("checks", out var checks), Is.True);
        Assert.That(checks.GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    public async Task HealthLive_IncludesForgeVersions()
    {
        var response = await _client.GetAsync("/health/live");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        var versions = json.GetProperty("versions");
        Assert.That(versions.TryGetProperty("Itenium.Forge.Core", out _), Is.True);
        Assert.That(versions.TryGetProperty("Itenium.Forge.HealthChecks", out _), Is.True);
    }

    #endregion

    #region Readiness Endpoint

    [Test]
    public async Task HealthReady_ReturnsJsonWithHealthyStatus()
    {
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        // Loki is disabled in Testing environment (appsettings.Testing.json)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(json.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task HealthReady_IncludesForgeSettings()
    {
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        var settings = json.GetProperty("settings");
        Assert.That(settings.GetProperty("serviceName").GetString(), Is.EqualTo("Forge.ExampleApp"));
        Assert.That(settings.GetProperty("application").GetString(), Is.EqualTo("ExampleApp"));
        Assert.That(settings.GetProperty("teamName").GetString(), Is.EqualTo("Core"));
        Assert.That(settings.GetProperty("tenant").GetString(), Is.EqualTo("itenium"));
    }

    [Test]
    public async Task HealthReady_IncludesChecks()
    {
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.That(json.TryGetProperty("checks", out var checks), Is.True);
        Assert.That(checks.GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    [Ignore("Loki may not be available in all test environments")]
    public async Task HealthReady_IncludesLokiCheck()
    {
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        var checks = json.GetProperty("checks");
        var lokiCheck = checks.EnumerateArray().FirstOrDefault(c => c.GetProperty("name").GetString() == "loki");
        Assert.That(lokiCheck.ValueKind, Is.Not.EqualTo(JsonValueKind.Undefined), "Loki health check should be present");
    }

    [Test]
    public async Task HealthReady_IncludesForgeVersions()
    {
        var response = await _client.GetAsync("/health/ready");
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);

        var versions = json.GetProperty("versions");
        Assert.That(versions.TryGetProperty("Itenium.Forge.Core", out _), Is.True);
        Assert.That(versions.TryGetProperty("Itenium.Forge.HealthChecks", out _), Is.True);
    }

    #endregion
}

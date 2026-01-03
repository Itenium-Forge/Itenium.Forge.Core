using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Itenium.Forge.ExampleApp.Tests;

[TestFixture]
public class SecureControllerTests
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

    #region Public Endpoint

    [Test]
    public async Task Public_WithoutToken_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/Secure/public");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Authenticated Endpoint

    [Test]
    public async Task Authenticated_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Secure/authenticated");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Authenticated_WithUserToken_ReturnsOk()
    {
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/authenticated");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Role-Based Endpoints

    [Test]
    public async Task UserOnly_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Secure/user-only");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task UserOnly_WithUserToken_ReturnsOk()
    {
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/user-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task AdminOnly_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Secure/admin-only");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AdminOnly_WithUserToken_ReturnsForbidden()
    {
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/admin-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AdminOnly_WithAdminToken_ReturnsOk()
    {
        var token = await TokenHelper.GetAdminTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/admin-only");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Capability-Based Endpoints - ReadResX

    [Test]
    public async Task ReadResX_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Secure/resx");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ReadResX_WithUserToken_ReturnsOk()
    {
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/resx");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ReadResX_WithAdminToken_ReturnsOk()
    {
        var token = await TokenHelper.GetAdminTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/resx");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Capability-Based Endpoints - WriteResX

    [Test]
    public async Task WriteResX_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/Secure/resx", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task WriteResX_WithUserToken_ReturnsForbidden()
    {
        // User does NOT have WriteResX capability
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Secure/resx");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task WriteResX_WithAdminToken_ReturnsOk()
    {
        // Admin has WriteResX capability
        var token = await TokenHelper.GetAdminTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Secure/resx");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    #endregion

    #region Custom Claims

    [Test]
    public async Task Department_WithAdminToken_ReturnsITDepartment()
    {
        var token = await TokenHelper.GetAdminTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/department");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.That(json.GetProperty("department").GetString(), Is.EqualTo("IT"));
    }

    [Test]
    public async Task Department_WithUserToken_ReturnsSalesDepartment()
    {
        var token = await TokenHelper.GetUserTokenAsync(_client);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Secure/department");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var content = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.That(json.GetProperty("department").GetString(), Is.EqualTo("Sales"));
    }

    #endregion
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace Itenium.Forge.Controllers.Tests;

[TestFixture]
public class CorsTests
{
    private static async Task<WebApplication> BuildAppAsync(string? corsOrigins = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        if (corsOrigins != null)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hosting:CorsOrigins"] = corsOrigins,
            });
        }

        builder.AddForgeControllers();

        var app = builder.Build();
        app.MapGet("/test", () => "ok");
        app.UseForgeControllers();

        await app.StartAsync();
        return app;
    }

    [Test]
    public async Task Get_WithAllowedOrigin_ReturnsAccessControlHeader()
    {
        await using var app = await BuildAppAsync("http://localhost:3000");
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
        Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin"), Contains.Item("http://localhost:3000"));
    }

    [Test]
    public async Task Get_WithDisallowedOrigin_NoAccessControlHeader()
    {
        await using var app = await BuildAppAsync("http://localhost:3000");
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "http://evil.com");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }

    [Test]
    public async Task Get_WithMultipleAllowedOrigins_AllowsEachOrigin()
    {
        await using var app = await BuildAppAsync("http://localhost:3000,http://localhost:3001");
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "http://localhost:3001");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.True);
    }

    [Test]
    public async Task Get_NoCorsConfiguration_NoAccessControlHeader()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }
}

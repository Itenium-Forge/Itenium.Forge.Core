using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Itenium.Forge.Controllers.Tests;

[TestFixture]
public class ResponseCompressionTests
{
    // A response large enough to exceed the compression threshold
    private const string LargePayload = "Forge response compression test. " +
                                        "This payload is deliberately long so that the compression " +
                                        "middleware has enough bytes to make compression worthwhile. " +
                                        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                                        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                                        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private static async Task<WebApplication> BuildAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddForgeControllers();

        var app = builder.Build();
        app.MapGet("/test", () => Results.Text(LargePayload));
        app.UseForgeControllers();

        await app.StartAsync();
        return app;
    }

    [Test]
    public async Task BrotliCompression_IsApplied_WhenClientAcceptsBrotli()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "br");

        var response = await client.GetAsync("/test");

        Assert.That(response.Content.Headers.ContentEncoding, Contains.Item("br"));
    }

    [Test]
    public async Task GzipCompression_IsApplied_WhenClientAcceptsGzip()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip");

        var response = await client.GetAsync("/test");

        Assert.That(response.Content.Headers.ContentEncoding, Contains.Item("gzip"));
    }

    [Test]
    public async Task BrotliCompression_TakesPrecedence_WhenClientAcceptsBoth()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, br");

        var response = await client.GetAsync("/test");

        Assert.That(response.Content.Headers.ContentEncoding, Contains.Item("br"));
    }

    [Test]
    public async Task NoCompression_WhenClientDoesNotAdvertiseEncoding()
    {
        await using var app = await BuildAppAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/test");

        Assert.That(response.Content.Headers.ContentEncoding, Is.Empty);
    }
}

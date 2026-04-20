using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.HttpClients.Tests;

[TestFixture]
public class ForwardedAuthorizationHandlerTests
{
    [Test]
    public async Task SendAsync_WithBearerToken_ForwardsAuthorizationHeader()
    {
        var request = await SendWithContext("Bearer eyJhbGciOiJSUzI1NiJ9.test");

        Assert.That(request.Headers.Authorization?.ToString(), Is.EqualTo("Bearer eyJhbGciOiJSUzI1NiJ9.test"));
    }

    [Test]
    public async Task SendAsync_NoHttpContext_DoesNotAddAuthorizationHeader()
    {
        var accessor = new HttpContextAccessor();
        var request = await SendWithHandler(new ForwardedAuthorizationHandler(accessor));

        Assert.That(request.Headers.Contains("Authorization"), Is.False);
    }

    [Test]
    public async Task SendAsync_ExistingAuthorizationHeader_IsNotOverwritten()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer original");

        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer inbound";
        var accessor = new HttpContextAccessor { HttpContext = context };

        await SendWithHandler(new ForwardedAuthorizationHandler(accessor), request);

        Assert.That(request.Headers.Authorization?.ToString(), Is.EqualTo("Bearer original"));
    }

    private static Task<HttpRequestMessage> SendWithContext(string authHeader)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = authHeader;
        var accessor = new HttpContextAccessor { HttpContext = context };
        return SendWithHandler(new ForwardedAuthorizationHandler(accessor));
    }

    private static async Task<HttpRequestMessage> SendWithHandler(
        ForwardedAuthorizationHandler handler,
        HttpRequestMessage? request = null)
    {
        request ??= new HttpRequestMessage(HttpMethod.Get, "http://example.com");
        handler.InnerHandler = new StubHttpHandler();
        await new HttpMessageInvoker(handler).SendAsync(request, CancellationToken.None);
        return request;
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage());
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Itenium.Forge.SecurityHeaders.Tests;

[TestFixture]
public class SecurityHeadersMiddlewareTests
{
    // ---------- pipeline ----------

    [Test]
    public async Task Middleware_AlwaysCallsNext()
    {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            new HeaderPolicyCollection());

        await middleware.Invoke(new DefaultHttpContext());

        Assert.That(nextCalled, Is.True);
    }

    // ---------- default policy — presence ----------

    [Test]
    public async Task DefaultPolicy_AddsXContentTypeOptions()
        => Assert.That(await InvokeWithDefaults(), Contains.Key("X-Content-Type-Options"));

    [Test]
    public async Task DefaultPolicy_AddsXFrameOptions()
        => Assert.That(await InvokeWithDefaults(), Contains.Key("X-Frame-Options"));

    [Test]
    public async Task DefaultPolicy_AddsReferrerPolicy()
        => Assert.That(await InvokeWithDefaults(), Contains.Key("Referrer-Policy"));

    [Test]
    public async Task DefaultPolicy_AddsPermissionsPolicy()
        => Assert.That(await InvokeWithDefaults(), Contains.Key("Permissions-Policy"));

    [Test]
    public async Task DefaultPolicy_AddsContentSecurityPolicy()
        => Assert.That(await InvokeWithDefaults(), Contains.Key("Content-Security-Policy"));

    [Test]
    public async Task DefaultPolicy_OmitsHsts_OverHttp()
    {
        var headers = await InvokeWithDefaults(isHttps: false);
        Assert.That(headers, Does.Not.ContainKey("Strict-Transport-Security"));
    }

    [Test]
    public async Task DefaultPolicy_AddsHsts_OverHttps()
    {
        var headers = await InvokeWithDefaults(isHttps: true);
        Assert.That(headers, Contains.Key("Strict-Transport-Security"));
    }

    // ---------- default policy — values ----------

    [Test]
    public async Task XContentTypeOptions_ValueIsNoSniff()
    {
        var headers = await InvokeWithDefaults();
        Assert.That(headers["X-Content-Type-Options"].ToString(), Is.EqualTo("nosniff"));
    }

    [Test]
    public async Task XFrameOptions_DefaultValueIsDeny()
    {
        var headers = await InvokeWithDefaults();
        Assert.That(headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
    }

    [Test]
    public async Task ReferrerPolicy_DefaultValueIsNoReferrer()
    {
        var headers = await InvokeWithDefaults();
        Assert.That(headers["Referrer-Policy"].ToString(), Is.EqualTo("no-referrer"));
    }

    [Test]
    public async Task PermissionsPolicy_DefaultValueDisablesCameraAndMicrophoneAndGeolocation()
    {
        var headers = await InvokeWithDefaults();
        Assert.That(headers["Permissions-Policy"].ToString(),
            Is.EqualTo("camera=(), microphone=(), geolocation=()"));
    }

    [Test]
    public async Task Hsts_DefaultValueIsOneYearWithSubDomains()
    {
        var headers = await InvokeWithDefaults(isHttps: true);
        Assert.That(headers["Strict-Transport-Security"].ToString(),
            Is.EqualTo("max-age=31536000; includeSubDomains"));
    }

    [Test]
    public async Task Hsts_WithoutIncludeSubDomains_OmitsSubDomainsDirective()
    {
        var headers = await Invoke(p => p.AddStrictTransportSecurity(includeSubDomains: false), isHttps: true);
        Assert.That(headers["Strict-Transport-Security"].ToString(),
            Does.Contain("max-age=").And.Not.Contain("includeSubDomains"));
    }

    // ---------- UseForgeSecurityHeaders extension ----------

    [Test]
    public async Task UseForgeSecurityHeaders_WithoutConfigure_MiddlewareIsRegisteredAndAppliesDefaultHeaders()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseForgeSecurityHeaders();
        app.MapGet("/", () => Results.Ok());
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/");

        Assert.That(response.Headers.Contains("X-Content-Type-Options"), Is.True);
        Assert.That(response.Headers.Contains("X-Frame-Options"), Is.True);
        Assert.That(response.Headers.Contains("Referrer-Policy"), Is.True);
        Assert.That(response.Headers.Contains("Permissions-Policy"), Is.True);
    }

    [Test]
    public async Task UseForgeSecurityHeaders_WithConfigure_OverridesDefaultPolicy()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseForgeSecurityHeaders(p => p.RemoveHeader("X-Frame-Options"));
        app.MapGet("/", () => Results.Ok());
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/");

        Assert.That(response.Headers.Contains("X-Content-Type-Options"), Is.True);
        Assert.That(response.Headers.Contains("X-Frame-Options"), Is.False);
    }

    // ---------- customisation ----------

    [Test]
    public async Task AddXFrameOptions_SameOrigin_SetsCorrectValue()
    {
        var headers = await Invoke(p => p.AddXFrameOptions("SAMEORIGIN"));
        Assert.That(headers["X-Frame-Options"].ToString(), Is.EqualTo("SAMEORIGIN"));
    }

    [Test]
    public async Task RemoveHeader_ExcludesHeaderFromResponse()
    {
        var headers = await Invoke(p => p.AddApiDefaults().RemoveHeader("X-Frame-Options"));
        Assert.That(headers, Does.Not.ContainKey("X-Frame-Options"));
    }

    [Test]
    public async Task EmptyPolicy_AddsNoHeaders()
    {
        var headers = await Invoke(_ => { });
        Assert.That(headers, Is.Empty);
    }

    // ---------- helpers ----------

    private static Task<IHeaderDictionary> InvokeWithDefaults(bool isHttps = false)
        => Invoke(p => p.AddApiDefaults(), isHttps);

    private static async Task<IHeaderDictionary> Invoke(
        Action<HeaderPolicyCollection> configure,
        bool isHttps = false)
    {
        var policy = new HeaderPolicyCollection();
        configure(policy);

        var context = new DefaultHttpContext();
        context.Request.Scheme = isHttps ? "https" : "http";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, policy);
        await middleware.Invoke(context);

        return context.Response.Headers;
    }
}

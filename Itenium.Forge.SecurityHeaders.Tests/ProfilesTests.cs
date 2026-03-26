using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Itenium.Forge.SecurityHeaders.Tests;

internal static class ProfileTestHelper
{
    internal static async Task<IHeaderDictionary> Invoke(
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

// ---------- ForApi profile ----------

[TestFixture]
public class ForApiProfileTests
{
    [Test]
    public async Task ForApi_AddsXContentTypeOptions()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForApi()), Contains.Key("X-Content-Type-Options"));

    [Test]
    public async Task ForApi_AddsXFrameOptions_Deny()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForApi());
        Assert.That(headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
    }

    [Test]
    public async Task ForApi_AddsReferrerPolicy_NoReferrer()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForApi());
        Assert.That(headers["Referrer-Policy"].ToString(), Is.EqualTo("no-referrer"));
    }

    [Test]
    public async Task ForApi_AddsPermissionsPolicy()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForApi()), Contains.Key("Permissions-Policy"));

    [Test]
    public async Task ForApi_AddsContentSecurityPolicy()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForApi()), Contains.Key("Content-Security-Policy"));

    [Test]
    public async Task ForApi_CspValue_IsDefaultSrcNoneFrameAncestorsNone()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForApi());
        Assert.That(headers["Content-Security-Policy"].ToString(),
            Is.EqualTo("default-src 'none'; frame-ancestors 'none'"));
    }

    [Test]
    public async Task ForApi_AddsHsts_OverHttps()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForApi(), isHttps: true), Contains.Key("Strict-Transport-Security"));

    [Test]
    public async Task ForApi_OmitsHsts_OverHttp()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForApi(), isHttps: false), Does.Not.ContainKey("Strict-Transport-Security"));
}

// ---------- ForBrowser profile ----------

[TestFixture]
public class ForBrowserProfileTests
{
    [Test]
    public async Task ForBrowser_AddsXContentTypeOptions()
        => Assert.That(await ProfileTestHelper.Invoke(p => p.ForBrowser()), Contains.Key("X-Content-Type-Options"));

    [Test]
    public async Task ForBrowser_AddsXFrameOptions_Deny()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForBrowser());
        Assert.That(headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
    }

    [Test]
    public async Task ForBrowser_ReferrerPolicy_IsStrictOriginWhenCrossOrigin()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForBrowser());
        Assert.That(headers["Referrer-Policy"].ToString(), Is.EqualTo("strict-origin-when-cross-origin"));
    }

    [Test]
    public async Task ForBrowser_CspValue_ContainsSelf()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForBrowser());
        Assert.That(headers["Content-Security-Policy"].ToString(), Does.Contain("'self'"));
    }

    [Test]
    public async Task ForBrowser_CspValue_ContainsDataUri()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForBrowser());
        Assert.That(headers["Content-Security-Policy"].ToString(), Does.Contain("data:"));
    }

    [Test]
    public async Task ForBrowser_CspValue_ContainsFrameAncestorsNone()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.ForBrowser());
        Assert.That(headers["Content-Security-Policy"].ToString(), Does.Contain("frame-ancestors 'none'"));
    }
}

// ---------- AddContentSecurityPolicy ----------

[TestFixture]
public class AddContentSecurityPolicyTests
{
    [Test]
    public async Task AddCsp_SetsCustomValue()
    {
        var headers = await ProfileTestHelper.Invoke(p => p.AddContentSecurityPolicy("default-src 'self'"));
        Assert.That(headers["Content-Security-Policy"].ToString(), Is.EqualTo("default-src 'self'"));
    }

    [Test]
    public async Task AddCsp_CalledTwice_KeepsLatestValue()
    {
        var headers = await ProfileTestHelper.Invoke(p => p
            .AddContentSecurityPolicy("default-src 'none'")
            .AddContentSecurityPolicy("default-src 'self'"));
        Assert.That(headers["Content-Security-Policy"].ToString(), Is.EqualTo("default-src 'self'"));
    }
}

// ---------- Path overrides ----------

[TestFixture]
public class PathPolicyOverrideTests
{
    [Test]
    public async Task PathOverride_ExactMatch_UsesOverridePolicy()
    {
        var headers = await InvokeWithPath("/swagger",
            defaultConfigure: p => p.ForApi(),
            pathConfigure: paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        Assert.That(headers, Does.Not.ContainKey("Content-Security-Policy"));
    }

    [Test]
    public async Task PathOverride_PrefixMatch_UsesOverridePolicy()
    {
        var headers = await InvokeWithPath("/swagger/index.html",
            defaultConfigure: p => p.ForApi(),
            pathConfigure: paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        Assert.That(headers, Does.Not.ContainKey("Content-Security-Policy"));
    }

    [Test]
    public async Task PathOverride_NonMatchingPath_UsesDefaultPolicy()
    {
        var headers = await InvokeWithPath("/api/data",
            defaultConfigure: p => p.ForApi(),
            pathConfigure: paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        Assert.That(headers, Contains.Key("Content-Security-Policy"));
    }

    [Test]
    public async Task PathOverride_LongestPrefixWins()
    {
        var headers = await InvokeWithPath("/swagger/v1/docs",
            defaultConfigure: p => p.ForApi(),
            pathConfigure: paths => paths
                .ForPath("/swagger", p => p.AddXFrameOptions("SAMEORIGIN"))
                .ForPath("/swagger/v1", p => p.AddXFrameOptions("DENY")));
        Assert.That(headers["X-Frame-Options"].ToString(), Is.EqualTo("DENY"));
    }

    [Test]
    public async Task PathOverride_CaseInsensitive_Matches()
    {
        var headers = await InvokeWithPath("/Swagger",
            defaultConfigure: p => p.ForApi(),
            pathConfigure: paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        Assert.That(headers, Does.Not.ContainKey("Content-Security-Policy"));
    }

    [Test]
    public async Task PathOverride_NullPathPolicies_UsesDefaultPolicy()
    {
        var policy = new HeaderPolicyCollection().ForApi();
        var context = new DefaultHttpContext();
        context.Request.Path = "/swagger";

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, policy);
        await middleware.Invoke(context);

        Assert.That(context.Response.Headers, Contains.Key("Content-Security-Policy"));
    }

    [Test]
    public async Task UseForgeSecurityHeaders_WithPathOverride_SwaggerPathRelaxed()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseForgeSecurityHeaders(
            p => p.ForApi(),
            paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        app.MapGet("/swagger", () => Results.Ok());
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/swagger");

        Assert.That(response.Headers.Contains("Content-Security-Policy"), Is.False);
        Assert.That(response.Headers.Contains("X-Content-Type-Options"), Is.True);
    }

    [Test]
    public async Task UseForgeSecurityHeaders_WithPathOverride_ApiPathStrict()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseForgeSecurityHeaders(
            p => p.ForApi(),
            paths => paths.ForPath("/swagger", p => p.ForApi().RemoveHeader("Content-Security-Policy")));
        app.MapGet("/api", () => Results.Ok());
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/api");

        Assert.That(response.Headers.Contains("Content-Security-Policy"), Is.True);
    }

    // ---------- helpers ----------

    private static async Task<IHeaderDictionary> InvokeWithPath(
        string path,
        Action<HeaderPolicyCollection> defaultConfigure,
        Action<PathPolicyCollection> pathConfigure)
    {
        var policy = new HeaderPolicyCollection();
        defaultConfigure(policy);

        var pathPolicies = new PathPolicyCollection();
        pathConfigure(pathPolicies);

        var context = new DefaultHttpContext();
        context.Request.Path = path;

        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, policy, pathPolicies);
        await middleware.Invoke(context);

        return context.Response.Headers;
    }
}

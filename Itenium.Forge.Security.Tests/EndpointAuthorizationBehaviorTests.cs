using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itenium.Forge.Security.Tests;

[TestFixture]
public class EndpointAuthorizationBehaviorTests
{
    // ---------- AllowAnonymousByDefault ----------

    [Test]
    public async Task AllowAnonymousByDefault_UnauthenticatedRequest_Returns200()
    {
        await using var app = BuildApp(
            Environments.Production,
            auth => auth.AllowAnonymousByDefault(),
            app => app.MapGet("/", () => Results.Ok()));
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // ---------- RequireAuthenticatedByDefault ----------

    [Test]
    public async Task RequireAuthenticated_UnauthenticatedRequest_Returns401()
    {
        await using var app = BuildApp(
            Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("user", p => p.RequireRole("user")),
            app => app.MapGet("/protected", () => Results.Ok()).RequireAuthorization("user"));
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/protected");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RequireAuthenticated_AllowAnonymousEndpoint_Returns200()
    {
        await using var app = BuildApp(
            Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("user", p => p.RequireRole("user")),
            app => app.MapGet("/public", () => Results.Ok()).AllowAnonymous());
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/public");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task RequireAuthenticated_AuthenticatedRequestWithMatchingPolicy_Returns200()
    {
        await using var app = BuildApp(
            Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("user", p => p.RequireRole("user")),
            app => app.MapGet("/protected", () => Results.Ok()).RequireAuthorization("user"));
        await app.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
        request.Headers.Add("X-Test-Auth", "true");
        request.Headers.Add("X-Test-Role", "user");

        var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task RequireAuthenticated_AuthenticatedRequestWithWrongPolicy_Returns403()
    {
        await using var app = BuildApp(
            Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin"))
                .AddPolicy("user", p => p.RequireRole("user")),
            app => app.MapGet("/admin", () => Results.Ok()).RequireAuthorization("admin"));
        await app.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "/admin");
        request.Headers.Add("X-Test-Auth", "true");
        request.Headers.Add("X-Test-Role", "user");

        var response = await app.GetTestClient().SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    // ---------- helper ----------

    private static WebApplication BuildApp(
        string environment,
        Action<ForgeAuthorizationBuilder> auth,
        Action<WebApplication> mapEndpoints)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddForgeSecurityCore(auth);
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", null);

        var app = builder.Build();
        app.UseForgeSecurity();
        mapEndpoints(app);
        return app;
    }
}

internal class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Test-Auth"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = Request.Headers["X-Test-Role"].FirstOrDefault() ?? "user";
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

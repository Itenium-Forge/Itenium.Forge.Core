using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Itenium.Forge.Security.Tests;

[TestFixture]
public class AuthorizationPolicyEnforcementTests
{
    // ---------- Development: hard crash ----------

    [Test]
    public void Development_WithNoAuthorizationConfigured_ThrowsOnUseForgeSecurity()
    {
        var app = BuildApp(Environments.Development, auth: null);

        Assert.Throws<InvalidOperationException>(() => app.UseForgeSecurity());
    }

    [Test]
    public void Development_RequireAuthenticatedByDefaultWithNoPolicies_ThrowsOnUseForgeSecurity()
    {
        var app = BuildApp(Environments.Development,
            auth => auth.RequireAuthenticatedByDefault());

        Assert.Throws<InvalidOperationException>(() => app.UseForgeSecurity());
    }

    [Test]
    public void Development_AllowAnonymousByDefault_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Development,
            auth => auth.AllowAnonymousByDefault());

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Development_AllowAnonymousByDefault_DoesNotEmitWarning()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Development,
            auth => auth.AllowAnonymousByDefault());
        app.UseForgeSecurity();

        Assert.That(logs, Is.Empty);
    }

    [Test]
    public void Development_RequireAuthenticatedByDefaultWithPolicies_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Development,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    // ---------- Non-Development: silent fallback ----------

    [Test]
    public void Production_WithNoAuthorizationConfigured_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Production, auth: null);

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Production_WithNoAuthorizationConfigured_EmitsError()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Production, auth: null);
        app.UseForgeSecurity();

        Assert.That(logs, Has.Some.Contains("No authorization policy configured"));
    }

    [Test]
    public void Production_WithNoAuthorizationConfigured_FallbackPolicyRequiresAuthenticatedUser()
    {
        var app = BuildApp(Environments.Production, auth: null);
        app.UseForgeSecurity();

        var authOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.That(authOptions.FallbackPolicy?.Requirements,
            Has.Some.InstanceOf<DenyAnonymousAuthorizationRequirement>());
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithNoPolicies_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Production,
            auth => auth.RequireAuthenticatedByDefault());

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithNoPolicies_DoesNotEmitWarning()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Production,
            auth => auth.RequireAuthenticatedByDefault());
        app.UseForgeSecurity();

        Assert.That(logs, Is.Empty);
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithNoPolicies_FallbackPolicyRequiresAuthenticatedUser()
    {
        var app = BuildApp(Environments.Production,
            auth => auth.RequireAuthenticatedByDefault());
        app.UseForgeSecurity();

        var authOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.That(authOptions.FallbackPolicy?.Requirements,
            Has.Some.InstanceOf<DenyAnonymousAuthorizationRequirement>());
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithPolicies_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithPolicies_DoesNotEmitWarning()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));
        app.UseForgeSecurity();

        Assert.That(logs, Is.Empty);
    }

    [Test]
    public void Production_RequireAuthenticatedByDefaultWithPolicies_FallbackPolicyRequiresAuthenticatedUser()
    {
        var app = BuildApp(Environments.Production,
            auth => auth
                .RequireAuthenticatedByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));
        app.UseForgeSecurity();

        var authOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.That(authOptions.FallbackPolicy?.Requirements,
            Has.Some.InstanceOf<DenyAnonymousAuthorizationRequirement>());
    }

    [Test]
    public void Production_AllowAnonymousByDefault_StartsSuccessfully()
    {
        var app = BuildApp(Environments.Production,
            auth => auth.AllowAnonymousByDefault());

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Production_AllowAnonymousByDefault_EmitsWarning()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Production,
            auth => auth.AllowAnonymousByDefault());
        app.UseForgeSecurity();

        Assert.That(logs, Has.Some.Contains("AllowAnonymous"));
    }

    [Test]
    public void Production_AllowAnonymousByDefault_FallbackPolicyIsNull()
    {
        var app = BuildApp(Environments.Production,
            auth => auth.AllowAnonymousByDefault());
        app.UseForgeSecurity();

        var authOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.That(authOptions.FallbackPolicy, Is.Null);
    }

    // ---------- AllowAnonymous + policies defined: documented as policies unused for fallback ----------

    [Test]
    public void Production_AllowAnonymousByDefaultWithPolicies_StartsSuccessfully()
    {
        // Policies are registered and usable via [Authorize(Policy = "...")] on individual endpoints,
        // but have no effect on the fallback — all endpoints remain publicly accessible by default.
        var app = BuildApp(Environments.Production,
            auth => auth
                .AllowAnonymousByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));

        Assert.DoesNotThrow(() => app.UseForgeSecurity());
    }

    [Test]
    public void Production_AllowAnonymousByDefaultWithPolicies_EmitsAnonymousWarning()
    {
        var (app, logs) = BuildAppWithLogs(Environments.Production,
            auth => auth
                .AllowAnonymousByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));
        app.UseForgeSecurity();

        Assert.That(logs, Has.Some.Contains("AllowAnonymous"));
        Assert.That(logs, Has.None.Contains("No authorization policy configured"));
    }

    [Test]
    public void Production_AllowAnonymousByDefaultWithPolicies_FallbackPolicyIsNull()
    {
        // Named policies ARE registered (accessible via [Authorize(Policy = "admin")]),
        // but the fallback is null — no authentication required by default.
        var app = BuildApp(Environments.Production,
            auth => auth
                .AllowAnonymousByDefault()
                .AddPolicy("admin", p => p.RequireRole("admin")));
        app.UseForgeSecurity();

        var authOptions = app.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        Assert.That(authOptions.FallbackPolicy, Is.Null);
    }

    // ---------- helpers ----------

    private static WebApplication BuildApp(
        string environment,
        Action<ForgeAuthorizationBuilder>? auth)
    {
        var (app, _) = BuildAppWithLogs(environment, auth);
        return app;
    }

    private static (WebApplication app, List<string> logs) BuildAppWithLogs(
        string environment,
        Action<ForgeAuthorizationBuilder>? auth)
    {
        var logs = new List<string>();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment
        });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new TestLoggerProvider(logs));
        builder.Services.AddForgeSecurityCore(auth);
        return (builder.Build(), logs);
    }
}

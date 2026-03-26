using Itenium.Forge.SecurityHeaders.Headers;
using Microsoft.AspNetCore.Builder;

namespace Itenium.Forge.SecurityHeaders;

public static class SecurityHeadersExtensions
{
    public static void UseForgeSecurityHeaders(
        this WebApplication app,
        Action<HeaderPolicyCollection>? configure = null,
        Action<PathPolicyCollection>? configurePaths = null)
    {
        var policy = new HeaderPolicyCollection().ForApi();
        configure?.Invoke(policy);

        PathPolicyCollection? pathPolicies = null;
        if (configurePaths is not null)
        {
            pathPolicies = new PathPolicyCollection();
            configurePaths(pathPolicies);
        }

        if (pathPolicies is null)
            app.UseMiddleware<SecurityHeadersMiddleware>(policy);
        else
            app.UseMiddleware<SecurityHeadersMiddleware>(policy, pathPolicies);
    }

    // ------------------------------------------------------------------
    // Profiles
    // ------------------------------------------------------------------

    /// <summary>
    /// Strict profile for JSON web APIs.
    /// Sets <c>Referrer-Policy: no-referrer</c> and <c>Content-Security-Policy: default-src 'none'</c>.
    /// </summary>
    public static HeaderPolicyCollection ForApi(this HeaderPolicyCollection policies) =>
        policies
            .AddXContentTypeOptions()
            .AddXFrameOptions()
            .AddReferrerPolicy("no-referrer")
            .AddStrictTransportSecurity()
            .AddPermissionsPolicy()
            .AddContentSecurityPolicy("default-src 'none'; frame-ancestors 'none'");

    /// <summary>
    /// Profile for applications that serve HTML (MVC / Razor Pages).
    /// Sets <c>Referrer-Policy: strict-origin-when-cross-origin</c> and a permissive-but-safe CSP.
    /// </summary>
    public static HeaderPolicyCollection ForBrowser(this HeaderPolicyCollection policies) =>
        policies
            .AddXContentTypeOptions()
            .AddXFrameOptions()
            .AddReferrerPolicy()
            .AddStrictTransportSecurity()
            .AddPermissionsPolicy()
            .AddContentSecurityPolicy(
                "default-src 'self'; script-src 'self'; style-src 'self'; " +
                "img-src 'self' data:; frame-ancestors 'none'");

    /// <summary>Alias for <see cref="ForApi"/>.</summary>
    public static HeaderPolicyCollection AddApiDefaults(this HeaderPolicyCollection policies) =>
        policies.ForApi();

    // ------------------------------------------------------------------
    // HeaderPolicyCollection builder methods
    // ------------------------------------------------------------------

    public static HeaderPolicyCollection AddXContentTypeOptions(this HeaderPolicyCollection policies)
    {
        policies["X-Content-Type-Options"] = new XContentTypeOptionsPolicy();
        return policies;
    }

    public static HeaderPolicyCollection AddXFrameOptions(
        this HeaderPolicyCollection policies,
        string value = "DENY")
    {
        policies["X-Frame-Options"] = new XFrameOptionsPolicy(value);
        return policies;
    }

    /// <summary>HTTPS only. Defaults to 1 year with <c>includeSubDomains</c>.</summary>
    public static HeaderPolicyCollection AddStrictTransportSecurity(
        this HeaderPolicyCollection policies,
        TimeSpan? maxAge = null,
        bool includeSubDomains = true)
    {
        policies["Strict-Transport-Security"] = new StrictTransportSecurityPolicy(
            maxAge ?? TimeSpan.FromDays(365), includeSubDomains);
        return policies;
    }

    public static HeaderPolicyCollection AddReferrerPolicy(
        this HeaderPolicyCollection policies,
        string value = "strict-origin-when-cross-origin")
    {
        policies["Referrer-Policy"] = new ReferrerPolicy(value);
        return policies;
    }

    public static HeaderPolicyCollection AddPermissionsPolicy(
        this HeaderPolicyCollection policies,
        string value = "camera=(), microphone=(), geolocation=()")
    {
        policies["Permissions-Policy"] = new PermissionsPolicy(value);
        return policies;
    }

    /// <summary>For standard profiles use <see cref="ForApi"/> or <see cref="ForBrowser"/>.</summary>
    public static HeaderPolicyCollection AddContentSecurityPolicy(
        this HeaderPolicyCollection policies,
        string value)
    {
        policies["Content-Security-Policy"] = new ContentSecurityPolicy(value);
        return policies;
    }

    public static HeaderPolicyCollection RemoveHeader(this HeaderPolicyCollection policies, string headerName)
    {
        policies.Remove(headerName);
        return policies;
    }
}

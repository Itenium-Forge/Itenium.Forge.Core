using Itenium.Forge.SecurityHeaders.Headers;
using Microsoft.AspNetCore.Builder;

namespace Itenium.Forge.SecurityHeaders;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds the security headers middleware to the pipeline with Forge API defaults.
    /// Call <paramref name="configure"/> to override or extend the default policy.
    /// </summary>
    public static void UseForgeSecurityHeaders(
        this WebApplication app,
        Action<HeaderPolicyCollection>? configure = null)
    {
        var policy = new HeaderPolicyCollection().AddApiDefaults();
        configure?.Invoke(policy);
        app.UseMiddleware<SecurityHeadersMiddleware>(policy);
    }

    // ------------------------------------------------------------------
    // HeaderPolicyCollection builder methods
    // ------------------------------------------------------------------

    /// <summary>Applies the default set of security headers suitable for a JSON web API.</summary>
    public static HeaderPolicyCollection AddApiDefaults(this HeaderPolicyCollection policies) =>
        policies
            .AddXContentTypeOptions()
            .AddXFrameOptions()
            .AddReferrerPolicy()
            .AddStrictTransportSecurity()
            .AddPermissionsPolicy();

    /// <summary>X-Content-Type-Options: nosniff</summary>
    public static HeaderPolicyCollection AddXContentTypeOptions(this HeaderPolicyCollection policies)
    {
        policies["X-Content-Type-Options"] = new XContentTypeOptionsPolicy();
        return policies;
    }

    /// <summary>X-Frame-Options: DENY (default) or SAMEORIGIN.</summary>
    public static HeaderPolicyCollection AddXFrameOptions(
        this HeaderPolicyCollection policies,
        string value = "DENY")
    {
        policies["X-Frame-Options"] = new XFrameOptionsPolicy(value);
        return policies;
    }

    /// <summary>Strict-Transport-Security: max-age=31536000; includeSubDomains (HTTPS only).</summary>
    public static HeaderPolicyCollection AddStrictTransportSecurity(
        this HeaderPolicyCollection policies,
        TimeSpan? maxAge = null,
        bool includeSubDomains = true)
    {
        policies["Strict-Transport-Security"] = new StrictTransportSecurityPolicy(
            maxAge ?? TimeSpan.FromDays(365), includeSubDomains);
        return policies;
    }

    /// <summary>Referrer-Policy: strict-origin-when-cross-origin (default).</summary>
    public static HeaderPolicyCollection AddReferrerPolicy(
        this HeaderPolicyCollection policies,
        string value = "strict-origin-when-cross-origin")
    {
        policies["Referrer-Policy"] = new ReferrerPolicy(value);
        return policies;
    }

    /// <summary>
    /// Permissions-Policy: disables camera, microphone and geolocation by default.
    /// Pass a custom value for full control, e.g. "camera=(), microphone=(), payment=()".
    /// </summary>
    public static HeaderPolicyCollection AddPermissionsPolicy(
        this HeaderPolicyCollection policies,
        string value = "camera=(), microphone=(), geolocation=()")
    {
        policies["Permissions-Policy"] = new PermissionsPolicy(value);
        return policies;
    }

    /// <summary>Removes a header from the policy (useful to suppress a default).</summary>
    public static HeaderPolicyCollection RemoveHeader(this HeaderPolicyCollection policies, string headerName)
    {
        policies.Remove(headerName);
        return policies;
    }
}

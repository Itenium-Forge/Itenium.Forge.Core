using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>
/// Strict-Transport-Security — instructs browsers to only use HTTPS.
/// Only applied to HTTPS responses; has no effect on plain HTTP.
/// </summary>
internal sealed class StrictTransportSecurityPolicy(int maxAgeSeconds, bool includeSubDomains) : IHeaderPolicy
{
    public void Apply(HttpContext context)
    {
        if (!context.Request.IsHttps) return;

        var value = $"max-age={maxAgeSeconds}";
        if (includeSubDomains) value += "; includeSubDomains";
        context.Response.Headers["Strict-Transport-Security"] = value;
    }
}

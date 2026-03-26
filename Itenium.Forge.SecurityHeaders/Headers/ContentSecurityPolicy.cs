using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>Content-Security-Policy — controls which resources the browser may load.</summary>
internal sealed class ContentSecurityPolicy(string value) : IHeaderPolicy
{
    public void Apply(HttpContext context)
        => context.Response.Headers.ContentSecurityPolicy = value;
}

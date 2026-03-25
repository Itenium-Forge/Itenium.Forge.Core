using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>Referrer-Policy — controls how much referrer information is included with requests.</summary>
internal sealed class ReferrerPolicy(string value) : IHeaderPolicy
{
    public void Apply(HttpContext context)
        => context.Response.Headers["Referrer-Policy"] = value;
}

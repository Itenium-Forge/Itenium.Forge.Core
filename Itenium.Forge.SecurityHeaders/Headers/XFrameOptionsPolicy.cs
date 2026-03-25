using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>X-Frame-Options — prevents clickjacking by controlling framing.</summary>
internal sealed class XFrameOptionsPolicy(string value) : IHeaderPolicy
{
    public void Apply(HttpContext context)
        => context.Response.Headers["X-Frame-Options"] = value;
}

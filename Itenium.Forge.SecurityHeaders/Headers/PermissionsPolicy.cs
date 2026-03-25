using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>Permissions-Policy — restricts which browser features the page can use.</summary>
internal sealed class PermissionsPolicy(string value) : IHeaderPolicy
{
    public void Apply(HttpContext context)
        => context.Response.Headers["Permissions-Policy"] = value;
}

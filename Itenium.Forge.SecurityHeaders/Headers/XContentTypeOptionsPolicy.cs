using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders.Headers;

/// <summary>X-Content-Type-Options: nosniff — prevents MIME-type sniffing.</summary>
internal sealed class XContentTypeOptionsPolicy : IHeaderPolicy
{
    public void Apply(HttpContext context)
        => context.Response.Headers.XContentTypeOptions = "nosniff";
}

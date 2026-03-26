using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders;

/// <summary>Writes a single HTTP response header.</summary>
public interface IHeaderPolicy
{
    void Apply(HttpContext context);
}

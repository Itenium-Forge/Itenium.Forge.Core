using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.SecurityHeaders;

/// <summary>
/// Applies the configured <see cref="HeaderPolicyCollection"/> to every response.
/// Headers are set before calling the next middleware so they are present on all
/// responses, including errors and redirects produced further down the pipeline.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HeaderPolicyCollection _policy;

    public SecurityHeadersMiddleware(RequestDelegate next, HeaderPolicyCollection policy)
    {
        _next = next;
        _policy = policy;
    }

    public Task Invoke(HttpContext context)
    {
        foreach (var policy in _policy.Values)
            policy.Apply(context);

        return _next(context);
    }
}

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
    private readonly PathPolicyCollection? _pathPolicies;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        HeaderPolicyCollection policy,
        PathPolicyCollection? pathPolicies = null)
    {
        _next = next;
        _policy = policy;
        _pathPolicies = pathPolicies;
    }

    public Task Invoke(HttpContext context)
    {
        // Headers are applied directly here, not inside a Response.OnStarting callback.
        //
        // NetEscapades.AspNetCore.SecurityHeaders uses OnStarting so that headers are
        // written at the last possible moment — after all downstream middleware has run —
        // ensuring nothing can overwrite them. This is a defensive pattern suited to a
        // general-purpose library where the author cannot know what consumers will add
        // downstream.
        //
        // For Forge, the startup order is prescribed: UseForgeSecurityHeaders is registered
        // before controllers, problem details, and Swagger. No Forge middleware overwrites
        // security headers downstream, so the OnStarting guarantee is overkill.
        //
        // Applying headers directly also means unit tests can use DefaultHttpContext
        // without a running Kestrel server — OnStarting callbacks are only fired by the
        // Kestrel pipeline, not by DefaultHttpContext.
        var active = _pathPolicies?.Resolve(context.Request.Path.Value ?? string.Empty)
                     ?? _policy;

        foreach (var policy in active.Values)
            policy.Apply(context);

        return _next(context);
    }
}

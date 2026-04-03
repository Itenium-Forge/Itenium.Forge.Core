using Microsoft.AspNetCore.Http;

namespace Itenium.Forge.HttpClients;

/// <summary>
/// Forwards the inbound Bearer token to outgoing downstream requests.
/// Registered automatically by <see cref="ForgeHttpClientExtensions.AddForgeHttpClient{T}"/>.
/// Has no effect when there is no active HTTP context (background jobs, scheduled tasks).
/// </summary>
internal sealed class ForwardedAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardedAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("Authorization"))
        {
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader))
                request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

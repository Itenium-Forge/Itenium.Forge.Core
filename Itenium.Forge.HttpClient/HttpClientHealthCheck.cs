using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.HttpClients;

/// <summary>
/// Health check that probes a downstream service's readiness endpoint.
/// Registered automatically by <see cref="ForgeHttpClientExtensions.AddForgeHttpClient{T}"/>.
/// </summary>
internal sealed class HttpClientHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName;
    private readonly string _healthCheckUrl;

    public HttpClientHealthCheck(IHttpClientFactory httpClientFactory, string clientName, string healthCheckUrl)
    {
        _httpClientFactory = httpClientFactory;
        _clientName = clientName;
        _healthCheckUrl = healthCheckUrl;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient(_clientName);
        try
        {
            using var response = await client.GetAsync(_healthCheckUrl, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Downstream responded with {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Unable to reach {_healthCheckUrl}", ex);
        }
    }
}

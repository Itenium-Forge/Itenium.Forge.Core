using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.Telemetry;

/// <summary>
/// Verifies the OTLP collector endpoint is reachable.
/// Any HTTP response (including 4xx) is treated as healthy — a refused connection is unhealthy.
/// </summary>
internal class OtlpHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _otlpEndpoint;

    public OtlpHealthCheck(IHttpClientFactory httpClientFactory, string otlpEndpoint)
    {
        _httpClientFactory = httpClientFactory;
        _otlpEndpoint = otlpEndpoint;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OtlpHealthCheck");
            await client.GetAsync(_otlpEndpoint, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"OTLP endpoint unreachable: {ex.Message}", ex);
        }
    }
}

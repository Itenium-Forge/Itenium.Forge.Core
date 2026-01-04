using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Itenium.Forge.Logging;

/// <summary>
/// Health check that verifies Grafana Loki is reachable.
/// </summary>
internal class LokiHealthCheck : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly HttpClient _httpClient;
    private readonly string _lokiUrl;

    public LokiHealthCheck(IHttpClientFactory httpClientFactory, string lokiUrl)
    {
        _httpClient = httpClientFactory.CreateClient("LokiHealthCheck");
        _httpClient.Timeout = Timeout;
        _lokiUrl = lokiUrl.TrimEnd('/');
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            var response = await _httpClient.GetAsync($"{_lokiUrl}/ready", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy($"Loki is reachable at {_lokiUrl}");
            }

            return HealthCheckResult.Unhealthy($"Loki returned {response.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"Loki check timed out after {Timeout.TotalSeconds}s");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Loki is not reachable at {_lokiUrl}", ex);
        }
    }
}

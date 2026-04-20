using System.ComponentModel.DataAnnotations;

namespace Itenium.Forge.HttpClients;

/// <summary>
/// Configuration for a single downstream HTTP dependency.
/// </summary>
public sealed class HttpClientEntryOptions
{
    /// <summary>Base URL of the downstream service, e.g. <c>https://api.example.com</c>.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Path probed for the readiness health check. Defaults to <c>/health/ready</c>.</summary>
    public string HealthPath { get; set; } = "/health/ready";

    /// <summary>Request timeout in seconds (1–300). Defaults to 30.</summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;

    internal string ResolvedHealthCheckUrl => BaseUrl.TrimEnd('/') + HealthPath;
}

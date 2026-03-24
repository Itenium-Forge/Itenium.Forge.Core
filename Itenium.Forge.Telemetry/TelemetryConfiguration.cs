namespace Itenium.Forge.Telemetry;

internal class TelemetryConfiguration
{
    /// <summary>OTLP gRPC endpoint, e.g. http://localhost:4317. Leave empty to disable OTLP export.</summary>
    public string OtlpEndpoint { get; set; } = "";

    /// <summary>Expose a Prometheus /metrics scrape endpoint. Defaults to true.</summary>
    public bool MetricsEnabled { get; set; } = true;
}

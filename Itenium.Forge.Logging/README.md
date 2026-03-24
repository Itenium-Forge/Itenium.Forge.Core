Itenium.Forge.Logging
======================

```sh
dotnet add package Itenium.Forge.Settings
dotnet add package Itenium.Forge.Logging
```

## Usage

```cs
using Itenium.Forge.Settings;
using Itenium.Forge.Logging;

Log.Logger = LoggingExtensions.CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<MyAppSettings>();
    builder.AddForgeLogging();

    var app = builder.Build();
    app.AddForgeLogging();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

## OpenTelemetry

Tracing is always active (so `Activity.Current` is populated per request and the W3C `traceparent` header is propagated). OTLP export and Prometheus metrics are opt-in via configuration.

```json
{
  "ForgeConfiguration": {
    "Observability": {
      "OtlpEndpoint": "http://localhost:4317",
      "MetricsEnabled": true
    }
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `OtlpEndpoint` | `""` | OTLP gRPC endpoint. Leave empty to disable export. |
| `MetricsEnabled` | `true` | Exposes `/metrics` for Prometheus scraping. |

When `OtlpEndpoint` is set, a health check named `otlp` is automatically registered (tagged `ready`).

The `traceparent` response header and trace context propagation to downstream `HttpClient` calls are handled automatically by the OTel SDK.

Every Serilog log entry is enriched with `TraceId` and `SpanId` from the current `Activity`, allowing you to correlate logs and traces in Grafana.

See the root `docker-compose.yml` to spin up a local Grafana + Tempo + Prometheus stack for functional testing.

---

Example `appsettings.json` when you want to override the default values:

```json
{
  "Serilog": {
    
  }
}
```

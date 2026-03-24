Itenium.Forge.Telemetry
=======================

```sh
dotnet add package Itenium.Forge.Telemetry
```

Adds OpenTelemetry tracing and metrics to a Forge application.

## Usage

```cs
using Itenium.Forge.Telemetry;

var builder = WebApplication.CreateBuilder(args);
builder.AddForgeLogging();   // Serilog — from Itenium.Forge.Logging
builder.AddForgeTelemetry(); // OpenTelemetry — this package

var app = builder.Build();
app.UseForgeLogging();
app.UseForgeTelemetry(); // exposes /metrics when MetricsEnabled = true

app.Run();
```

## Configuration

```json
{
  "ForgeConfiguration": {
    "Telemetry": {
      "OtlpEndpoint": "http://localhost:4317",
      "MetricsEnabled": true
    }
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `OtlpEndpoint` | `""` | OTLP gRPC endpoint. Leave empty to disable OTLP export. |
| `MetricsEnabled` | `true` | Exposes `/metrics` for Prometheus scraping. |

When `OtlpEndpoint` is set, a health check named `otlp` is registered (tagged `ready`).

## What's included

- **Tracing** — always active (no config needed). `Activity.Current` is populated per request, the W3C `traceparent` header is propagated to all downstream `HttpClient` calls.
- **OTLP export** — sends traces (and metrics when enabled) to any OTLP-compatible collector (Grafana Tempo, Jaeger, etc.).
- **Prometheus metrics** — exposes `/metrics` via `OpenTelemetry.Exporter.Prometheus.AspNetCore`.
- **Runtime metrics** — thread pool, GC, memory via `OpenTelemetry.Instrumentation.Runtime`.
- **Health check** — verifies the OTLP collector is reachable; degraded status when not.

---

## Functional Testing

See the [root README — Functional Testing section](../README.md#functional-testing--telemetry) for a step-by-step walkthrough covering:

1. Generating traffic via Swagger or curl
2. Verifying `/metrics` output
3. Inspecting traces in Grafana Tempo
4. Proving the `traceparent` → `traceId` round-trip in ProblemDetails
5. Checking `/health/ready` for the `otlp` health check
6. Correlating logs with traces via `TraceId` enrichment

The local observability stack (Grafana + Tempo + Prometheus + Loki) is started with:

```sh
docker-compose up -d        # from the repo root
dotnet run --project Itenium.Forge.ExampleApp
```

---

See [DECISIONS.md](../DECISIONS.md) for the full architectural decision log (ADR-001, ADR-002, ADR-003).

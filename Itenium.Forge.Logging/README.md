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

## Trace correlation

Every Serilog log entry is enriched with `TraceId` and `SpanId` from `Activity.Current`, allowing you to correlate logs with traces in Grafana Tempo.

`CorrelationIdMiddleware` ensures a W3C trace ID is available for every request. It reads the incoming `traceparent` header when present (continuing an existing distributed trace) or generates a fresh one when absent. The trace ID is also written to `HttpContext.TraceIdentifier`, so it appears in ProblemDetails responses automatically.

Outgoing `HttpClient` calls (resolved from `IHttpClientFactory`) automatically forward the `traceparent` header to downstream services.

For OTLP export and Prometheus metrics, add `Itenium.Forge.Telemetry` — see that package's README for configuration.

---

Example `appsettings.json` when you want to override the default values:

```json
{
  "Serilog": {

  }
}
```

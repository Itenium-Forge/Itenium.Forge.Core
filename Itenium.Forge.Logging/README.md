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

## Field masking

Sensitive field values are replaced with `***` before they reach any log sink.
Masking operates at two layers that share the same `FieldMaskingOptions` configuration.

### HTTP layer — request body, query string, and headers

`RequestLoggingMiddleware` masks matching field names in the raw JSON body (recursive)
and query-string parameters before logging. Headers are opt-in via an allowlist.

```csharp
builder.AddForgeLogging(options =>
{
    options.AddMaskedFields("iban", "dob");          // extend body/query blocklist
    options.SetMaskedFields("iban", "dob");          // replace body/query blocklist entirely

    options.AddAllowedHeaders("X-Custom-Header");    // allow header to be logged
    options.AddMaskedHeaders("X-Api-Secret");        // mask header value when logged
});
```

Default masked fields: `password`, `passwd`, `token`, `secret`, `authorization`,
`client_secret`, `api_key`, `access_token`, `refresh_token`.

Default allowed headers (only these are logged): `Content-Type`, `Accept`, `X-Forwarded-For`, `traceparent`.

Default masked headers (logged as `***`): `Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key`.

Header decision tree:

```
header in AllowedHeaders?
    NO  → not logged at all
    YES → in MaskedHeaders?
            NO  → logged with real value
            YES → logged as ***
```

### Model layer — logged objects

When an object is logged directly with `_logger.LogInformation("{@obj}", obj)`, the same
body/query blocklist is applied automatically via a Serilog destructure policy.

For fields that are sensitive only on a specific type (e.g. GDPR fields like `Name` or
`Address` that are not universally sensitive), implement `IObjectMasker<T>` on the model:

```csharp
public class UserProfile : IObjectMasker<UserProfile>
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }

    public IEnumerable<Expression<Func<UserProfile, object>>> GetMaskedFields()
    {
        yield return obj => obj.Name;
        yield return obj => obj.Address;
    }
}
```

### When to use which

| Goal | Mechanism |
|---|---|
| Mask `password` everywhere | Default blocklist — already included |
| Mask `iban` in body/query and all logged objects | `options.AddMaskedFields("iban")` |
| Mask `name` only on `UserProfile` | `IObjectMasker<UserProfile>` |
| Log `Content-Type` header | Default allowlist — already included |
| Log `Authorization` header (as `***`) | `options.AddAllowedHeaders("Authorization")` |

---

Example `appsettings.json` when you want to override the default values:

```json
{
  "Serilog": {

  }
}
```

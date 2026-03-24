Itenium.Forge.Core
==================

- Github > Profile > Settings > Developer settings
  - Personal access tokens > Tokens (classic)
  - Generate new token (classic)
    - Scopes: `read:packages`


```sh
dotnet nuget add source --username OWNER --password TOKEN --store-password-in-clear-text --name itenium "https://nuget.pkg.github.com/Itenium-Forge/index.json"
```

See [nuget credentials](https://github.com/dotnet/dotnet-docker/blob/main/documentation/scenarios/nuget-credentials.md) on how to use this in a Docker build.


See [DECISIONS.md](./DECISIONS.md) for the architectural decision log.

## Packages

- [Itenium.Forge.Core](./Itenium.Forge.Core/README.md)
- [Itenium.Forge.Settings](./Itenium.Forge.Settings/README.md)
- [Itenium.Forge.Logging](./Itenium.Forge.Logging/README.md)
- [Itenium.Forge.Telemetry](./Itenium.Forge.Telemetry/README.md)
- [Itenium.Forge.Swagger](./Itenium.Forge.Swagger/README.md)
- [Itenium.Forge.Controllers](./Itenium.Forge.Controllers/README.md)
- [Itenium.Forge.Security](./Itenium.Forge.Security/README.md)


## Local Development — Observability Stack

Forge ships a `docker-compose.yml` that starts the full **LGTM** stack (Loki, Grafana, Tempo, Prometheus) in a single container using `grafana/otel-lgtm`.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) running

### Start the stack

```sh
docker-compose up -d
```

| Service    | URL                        | Purpose                        |
|------------|----------------------------|--------------------------------|
| Grafana    | http://localhost:3000      | Dashboards (no login required) |
| Prometheus | http://localhost:9090      | Metrics explorer               |
| OTLP gRPC  | http://localhost:4317      | Trace + metric ingest          |
| OTLP HTTP  | http://localhost:4318      | Alternative OTLP transport     |

### Start the example app

```sh
dotnet run --project Itenium.Forge.ExampleApp
```

The app sends traces to Tempo and metrics to Prometheus via OTLP on port 4317.

### Stop the stack

```sh
docker-compose down
```

---

## Functional Testing — Telemetry

The steps below verify each signal (traces, metrics, logs, health) after starting both the Docker stack and the example app.

### 1. Generate traffic

Open the Swagger UI at **http://localhost:5000/swagger** and fire a few requests — any endpoint works. The `/api/problem/bad-request` endpoint is useful because it returns a ProblemDetails body with a `traceId` field.

Alternatively use curl:

```sh
curl -s http://localhost:5000/api/problem/bad-request | jq .
```

Expected response fragment:
```json
{
  "status": 400,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The `traceId` value is the 32-char OTel trace ID for that request. Note it down — you can paste it into Grafana Tempo to jump straight to the trace.

### 2. Verify metrics

```sh
curl http://localhost:5000/metrics
```

You should see Prometheus text output including lines like:

```
http_server_request_duration_seconds_count{...} 3
http_server_active_requests{...} 0
process_runtime_dotnet_gc_collections_count_total{...} 1
```

You can also browse **http://localhost:9090** (Prometheus UI), type `http_server_request_duration_seconds_count` into the query box, and click **Execute**.

### 3. Verify traces in Grafana Tempo

1. Open **http://localhost:3000**
2. Go to **Dashboards → ASP.NET Core — OpenTelemetry** to see the pre-provisioned dashboard (request rate, latency, error rate, GC, thread pool).
3. To inspect a specific trace: go to **Explore**, select the **Tempo** data source, paste the `traceId` from step 1 into the **TraceID** field, and click **Run query**. You will see the full span tree for that request.

### 4. Verify the traceparent round-trip

Send a request with a `traceparent` header to prove the trace ID flows from the header into the ProblemDetails response body:

```sh
curl -s \
  -H "traceparent: 00-aabbccddeeff00112233445566778899-0011223344556677-01" \
  http://localhost:5000/api/problem/bad-request | jq .traceId
```

Expected output:
```
"aabbccddeeff00112233445566778899"
```

The middleware reads `Activity.Current.TraceId` (which the OTel SDK set from the incoming `traceparent` header) and writes it to `HttpContext.TraceIdentifier`, which ProblemDetails then picks up.

### 5. Verify health checks

```sh
curl -s http://localhost:5000/health/ready | jq .
```

Expected: `"status": "Healthy"` with an `otlp` entry in the `checks` array showing the collector is reachable.

```sh
curl -s http://localhost:5000/health/live | jq .
```

The liveness endpoint does not include external dependency checks — it should always return Healthy as long as the app is running.

### 6. Verify log enrichment

The app writes logs to `logs/` (file) and to the console. Each log line emitted during a request will contain `TraceId` and `SpanId` matching the OTel trace. Look for output like:

```
[INF] GET /api/problem/bad-request {"TraceId": "4bf92f3577b34da6a3ce929d0e0e4736", "SpanId": "00f067aa0ba902b7", ...}
```

In Grafana, go to **Explore → Loki** (if `LokiUrl` is configured) and filter by `TraceId` to correlate logs with the Tempo trace from step 3.

---

## TODO

- Template Repository
- Roslynator
  - Warnings as Errors
- Settings:
  - Settings 
  - Secrets in Vault/Consul
- Http:
  - x-correlation-id header forwarding
  - Circuit Breaker



### Database Migrations

```cs
public static void MigrateDb(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
    dbContext.Database.Migrate();
}

/// <summary>
/// For EF Migrations
/// </summary>
internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureDbContextBuilder(builder);
        return new AppDbContext(builder.Options);
    }
}
```


### Https Enforcement

Is it enough to just have the reverse proxy take care of this?

```cs
builder.Services.AddHttpsRedirection(options => { options.HttpsPort = 443; });
app.UseHttpsRedirection();
app.UseHsts();
```

### Rate Limiting

```cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 100; // 100 requests/minute
    });
});
```

### Request Validation

We need to have the validation on the frontend and on the backend.
- Just duplicate it?
- Use something like zod and generate something for the backend?
  - Or write in CSharp and generate something for the frontend?
- Have the .NET code call a Node docker that contains the same logic?

Also other features:
- Translated error messages
- List all errors


```cs
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});
```


### Others

```cs
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(ttcSettings.PublicImageFolder),
    RequestPath = "/img"
});
```

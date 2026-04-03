Itenium.Forge.HttpClient
========================

```sh
dotnet add package Itenium.Forge.HttpClient
```

Configures typed Refit clients with Forge defaults:
- **BaseUrl** resolved per environment via `appsettings.{Environment}.json`
- **traceparent** header propagated automatically (via `AddForgeLogging`)
- **Bearer token forwarded** automatically from the inbound request to the downstream call
- **Health check** auto-registered per client, named `http-{name}`, tagged `ready`
- **ValidateOnStart** — startup crashes if any config property fails validation (`BaseUrl` missing/malformed, `TimeoutSeconds` out of range)

## Usage

```csharp
builder.AddForgeHttpClient<IMyServiceClient>("MyService");
```

```json
// appsettings.json
"ForgeConfiguration": {
  "HttpClients": {
    "MyService": {
      "BaseUrl": "http://localhost:5100",
      "HealthPath": "/health/ready",
      "TimeoutSeconds": 30
    }
  }
}

// appsettings.Production.json
"ForgeConfiguration": {
  "HttpClients": {
    "MyService": {
      "BaseUrl": "https://myservice.prd.itenium.be"
    }
  }
}
```

| Property | Required | Default | Description |
|----------|:--------:|---------|-------------|
| `BaseUrl` | ✓ | — | Base URL, must be a valid URL |
| `HealthPath` | | `/health/ready` | Path used for the readiness health check |
| `TimeoutSeconds` | | `30` | Request timeout (1–300 s) |

## Service-to-service authentication

`AddForgeHttpClient<T>()` automatically forwards the inbound `Authorization: Bearer` token to every outgoing request via `ForwardedAuthorizationHandler`. The downstream service sees the original caller's identity.

**Limitations of token forwarding:**
- No inbound HTTP context (background jobs, `IHostedService`) → no token is forwarded
- Audiences must overlap; a strict JWT validator will reject a token not issued for the downstream audience

Client credentials (service-to-service without a user context) are not yet implemented (tracked as B8).

## Health check

Each `AddForgeHttpClient<T>("MyService")` call registers a readiness check named `http-MyService`.
To exclude it — for example in tests — remove it from `HealthCheckServiceOptions` after registration:

```csharp
services.Configure<HealthCheckServiceOptions>(opts =>
    opts.Registrations.RemoveAll(r => r.Name == "http-MyService"));
```

## Refit interface

```csharp
[Headers("Accept: application/json")]
public interface IMyServiceClient
{
    [Get("/items")]
    Task<IReadOnlyList<Item>> GetItemsAsync(CancellationToken ct = default);

    [Get("/items/{id}")]
    Task<Item> GetItemAsync(int id, CancellationToken ct = default);
}
```

## Client NuGet package

Each Forge service ships a thin `*.Client` NuGet containing only the Refit interface and DTOs — no server code. Consuming apps install it and register via `AddForgeHttpClient<T>`. Refit generates the HTTP implementation at compile time via source generators.

The `*.Client` project is packable (`PackageId` set, no `IsPackable=false`) and is published automatically by the `publish.yaml` workflow on version tags alongside the other Forge packages.

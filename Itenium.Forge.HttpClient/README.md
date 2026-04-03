Itenium.Forge.HttpClient
========================

```sh
dotnet add package Itenium.Forge.HttpClient
```

Configures typed Refit clients with Forge defaults:
- **BaseUrl** resolved per environment via `appsettings.{Environment}.json`
- **traceparent** header propagated automatically (via `AddForgeLogging`)
- **Health check** auto-registered per client, tagged `ready`
- **ValidateOnStart** — startup crashes if `BaseUrl` is missing or malformed

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

`AddForgeHttpClient<T>()` sends **no credentials** to the downstream service. This is fine while the downstream service is open, but any Forge service that calls `RequireAuthenticatedByDefault()` will reject the call with **401 Unauthorized**.

Two standard patterns exist; neither is implemented yet (tracked as B8):

**Token forwarding (on-behalf-of)** — forward the inbound user Bearer token downstream:
- Downstream sees the *user's* identity; useful when it enforces per-user permissions
- Does not work for background jobs or scheduled tasks (no inbound HTTP context)
- Requires audiences to overlap, otherwise strict JWT validation rejects the token

**Client credentials** — ExampleApp authenticates to the downstream service as itself:
- Works for all call sites including background jobs
- Downstream sees the *service's* identity, not the user's
- Requires client registration in the identity server and secret management

Until B8 is implemented, downstream Forge services should remain open (`no AddForgeSecurity*`) or use a custom `DelegatingHandler` to attach credentials.

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

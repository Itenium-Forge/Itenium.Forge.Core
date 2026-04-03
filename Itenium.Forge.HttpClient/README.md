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

## How Refit works

Refit generates the `HttpClient` implementation at compile time from the interface. There is no manual implementation to write — declaring the interface and its attributes is sufficient.

```csharp
// This is all that's needed — Refit generates the rest
[Headers("Accept: application/json")]
public interface IMyServiceClient
{
    [Get("/items")]
    Task<IReadOnlyList<Item>> GetItemsAsync(CancellationToken ct = default);

    [Get("/items/{id}")]
    Task<Item> GetItemAsync(int id, CancellationToken ct = default);
}
```

### Error handling

| Scenario | Exception thrown |
|----------|-----------------|
| Non-2xx response (400, 401, 404, 500, …) | `Refit.ApiException` |
| 422 Unprocessable Entity | `Refit.ValidationApiException` (subclass of `ApiException`) |
| JSON deserialization failure | `System.Text.Json.JsonException` |
| Timeout (`TimeoutSeconds` exceeded) | `TaskCanceledException` |
| Network unreachable | `HttpRequestException` |

`ApiException` exposes `StatusCode`, `ReasonPhrase`, `Content` (raw response body), and `Headers`.
`ValidationApiException` additionally exposes `Content` parsed as `ValidationProblemDetails`.

```csharp
try
{
    var item = await _client.GetItemAsync(id, ct);
}
catch (ValidationApiException ex)
{
    // 422 — downstream returned a validation problem
    logger.LogWarning("Validation error from MyService: {Errors}", ex.Content?.Errors);
}
catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    return NotFound();
}
catch (ApiException ex)
{
    logger.LogError(ex, "MyService returned {Status}", ex.StatusCode);
    throw;
}
```

> `ValidationApiException` is a subclass of `ApiException` — catch it first.

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

## Shipping a client NuGet package

Each Forge service ships a thin `*.Client` NuGet containing only the Refit interface and DTOs — no server code. Refit generates the HTTP implementation at compile time in the consuming project.

### Steps to create a client package

**1. Create the project**

```sh
dotnet new classlib -n MyCompany.MyService.Client
```

**2. Configure the `.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>MyCompany.MyService.Client</PackageId>
    <Description>Refit client for MyService.</Description>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="README.md" Pack="true" PackagePath="" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Refit.HttpClientFactory" Version="..." />
  </ItemGroup>
</Project>
```

**3. Add the interface and DTOs**

```csharp
// DTOs
public sealed record Item(int Id, string Name);

// Refit interface
[Headers("Accept: application/json")]
public interface IMyServiceClient
{
    [Get("/items")]
    Task<IReadOnlyList<Item>> GetItemsAsync(CancellationToken ct = default);
}
```

**4. Add the project to the solution and publish pipeline**

The `publish.yaml` workflow runs `dotnet pack` on all packable projects and publishes them to GitHub Packages on version tags (`v*.*.*`). No additional pipeline changes are needed — the client package is picked up automatically.

**5. Consuming the package**

```sh
dotnet add package MyCompany.MyService.Client
```

```csharp
// Program.cs
builder.AddForgeHttpClient<IMyServiceClient>("MyService");
```

> The manual part is steps 1–3: creating the project, writing the interface, and adding it to the solution. Publishing is fully automatic.

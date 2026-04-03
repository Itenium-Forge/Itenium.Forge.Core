Itenium.Forge.HealthChecks
==========================

```sh
dotnet add package Itenium.Forge.HealthChecks
```

## Usage

```cs
using Itenium.Forge.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddForgeHealthChecks();

var app = builder.Build();
app.UseForgeHealthChecks();
```

## Endpoints

- `/health/live` - Liveness probe (fast, no dependency checks)
- `/health/ready` - Readiness probe (includes dependency checks)

Both endpoints include ForgeSettings metadata in the response.

## Adding Custom Health Checks

```cs
builder.AddForgeHealthChecks()
    .AddNpgSql(connectionString, tags: ["ready"])
    .AddRedis(redisConnectionString, tags: ["ready"]);
```

Use the `live` tag for checks that should run on the liveness endpoint,
and the `ready` tag for checks that should run on the readiness endpoint.

`AddForgeHttpClient<T>("MyService")` (from `Itenium.Forge.HttpClient`) automatically
registers a readiness check named `http-MyService`. To exclude a check — for example in tests:

```cs
services.Configure<HealthCheckServiceOptions>(opts =>
    opts.Registrations.RemoveAll(r => r.Name == "http-MyService"));
```

## Response Format

```json
{
  "status": "Healthy",
  "service": "my-api",
  "application": "MyApp",
  "environment": "Production",
  "tenant": "CustomerX",
  "team": "CoreTeam",
  "checks": [
    { "name": "self", "status": "Healthy", "duration": "00:00:00.001" }
  ]
}
```

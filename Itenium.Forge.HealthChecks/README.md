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

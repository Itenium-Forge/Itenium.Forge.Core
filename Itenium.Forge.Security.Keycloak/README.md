Itenium.Forge.Security.Keycloak
================================

JWT Bearer authentication configured for Keycloak.

## Installation

```sh
dotnet add package Itenium.Forge.Security.Keycloak
```

## Configuration

Add the Security section to your `appsettings.json`:

```json
{
  "ForgeConfiguration": {
    "Security": {
      "Authority": "http://localhost:8080/realms/itenium",
      "Audience": "forge-api",
      "RequireHttpsMetadata": false
    }
  }
}
```

| Property | Description |
|----------|-------------|
| `Authority` | The Keycloak realm URL |
| `Audience` | The expected audience claim (client ID) |
| `RequireHttpsMetadata` | Set to `false` for local development |

## Usage

```cs
using Itenium.Forge.Security;
using Itenium.Forge.Security.Keycloak;

var builder = WebApplication.CreateBuilder(args);

// Add Keycloak authentication
builder.AddForgeKeycloak();

var app = builder.Build();

// Add security middleware
app.UseForgeSecurity();

app.Run();
```

## Built-in Policies

| Policy | Description |
|--------|-------------|
| `admin` | Requires the `admin` role |
| `user` | Requires the `user` role |

## Keycloak Setup

See the `Itenium.Security` directory for a docker-compose setup with pre-configured Keycloak.

### Test Users

| Username | Password | Roles |
|----------|----------|-------|
| admin | admin | admin, user |
| user | user | user |

### Get a Token

```bash
curl -X POST http://localhost:8080/realms/itenium/protocol/openid-connect/token \
  -d "grant_type=password" \
  -d "client_id=forge-spa" \
  -d "username=admin" \
  -d "password=admin"
```

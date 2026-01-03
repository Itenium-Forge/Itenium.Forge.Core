Itenium.Forge.Security
======================

JWT Bearer authentication configured for Keycloak with policy-based authorization.

## Installation

```sh
dotnet add package Itenium.Forge.Security
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

var builder = WebApplication.CreateBuilder(args);

// Add security services
builder.AddForgeSecurity();

var app = builder.Build();

// Add authentication/authorization middleware
app.UseForgeSecurity();

app.Run();
```

## Authorization

Use the `[Authorize]` attribute to protect endpoints:

```cs
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    private readonly ICurrentUser _currentUser;

    public SecureController(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    // Requires any authenticated user
    [Authorize]
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        return Ok(new { userName = _currentUser.UserName });
    }

    // Requires the 'admin' role
    [Authorize(Policy = "admin")]
    [HttpGet("admin")]
    public IActionResult AdminOnly()
    {
        return Ok("Admin access granted");
    }
}
```

## Built-in Policies

| Policy | Description |
|--------|-------------|
| `admin` | Requires the `admin` role |
| `user` | Requires the `user` role |

## ICurrentUser

Inject `ICurrentUser` to access the current user's information:

```cs
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
```

## Logging

User information is automatically added to the Serilog log context:
- `UserId`
- `UserName`

## Keycloak Setup

See the `Itenium.Security` directory for a docker-compose setup with pre-configured Keycloak.

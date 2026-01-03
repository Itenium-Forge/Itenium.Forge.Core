Itenium.Forge.Security
======================

Common security abstractions for Forge applications.

**This package provides shared interfaces and middleware. You also need one of:**
- `Itenium.Forge.Security.Keycloak` - For external Keycloak/OIDC provider
- `Itenium.Forge.Security.OpenIddict` - For self-hosted identity server

## Installation

```sh
dotnet add package Itenium.Forge.Security
dotnet add package Itenium.Forge.Security.Keycloak   # OR
dotnet add package Itenium.Forge.Security.OpenIddict
```

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

## Usage

```cs
[Authorize]
[HttpGet("profile")]
public IActionResult GetProfile([FromServices] ICurrentUser currentUser)
{
    return Ok(new { userName = currentUser.UserName });
}

[Authorize(Policy = "admin")]
[HttpGet("admin")]
public IActionResult AdminOnly()
{
    return Ok("Admin access granted");
}
```

## Logging

User information is automatically added to the Serilog log context:
- `UserId`
- `UserName`

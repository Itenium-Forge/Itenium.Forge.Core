# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test project
dotnet test Itenium.Forge.Settings.Tests
dotnet test Itenium.Forge.Logging.Tests
dotnet test Itenium.Forge.ExampleApp.Tests

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Pack NuGet packages
dotnet pack --configuration Release

# Run the example app
dotnet run --project Itenium.Forge.ExampleApp
```

## Architecture

This is a set of opinionated NuGet packages ("Forge") for bootstrapping .NET 10 web APIs. Each package is a thin wrapper that configures a specific concern via `Add*`/`Use*` extension methods on `WebApplicationBuilder`/`WebApplication`.

### Projects & Dependency Flow

```
Core (ForgeSettings POCO)
  ├── Settings (IForgeSettings, appsettings loading)
  ├── Logging (Serilog bootstrap, request logging, Loki)
  ├── Controllers (MVC, CORS, ProblemDetails RFC 7807)
  ├── Swagger (Swashbuckle config)
  ├── HealthChecks (/health/live, /health/ready)
  └── Security (ICurrentUser, claims extraction)
        ├── Security.Keycloak (OIDC/JWT)
        └── Security.OpenIddict (self-hosted identity server, EF Core)
```

### Startup Pattern (see ExampleApp/Program.cs)

Consuming apps follow a fixed registration order:
1. `AddForgeSettings<TSettings>()` — loads `appsettings.json` + environment overlay
2. `AddForgeLogging()` / `UseForgeLogging()`
3. `AddForgeSecurity*()` / `UseForgeSecurity()`
4. `AddForgeControllers()` / `UseForgeControllers()`
5. `AddForgeProblemDetails()` / `UseForgeProblemDetails()`
6. `AddForgeSwagger()` / `UseForgeSwagger()`
7. `AddForgeHealthChecks()` / `UseForgeHealthChecks()`

### Key Interfaces

- **`IForgeSettings`** — app settings contract; implementations must expose a `ForgeSettings Forge` property with service metadata (ServiceName, TeamName, Tenant, Environment, Application).
- **`ICurrentUser`** — provides UserId, UserName, Email, Roles, IsAuthenticated from JWT claims.

### Testing Conventions

- **NUnit 4** with `[TestFixture]`/`[Test]` attributes.
- Integration tests use `WebApplicationFactory<Program>` — see `ExampleAppFactory` which swaps in SQLite in-memory and removes external health checks.
- `InternalsVisibleTo` is configured for `Itenium.Forge.Settings` → its test projects.

## Build Configuration

- **Target:** net10.0, nullable enabled, `TreatWarningsAsErrors: true`
- **Central package management** via `Directory.Packages.props` — add version there, not in individual csproj files.
- **Solution format:** `.slnx` (modern XML format).
- Packable projects produce NuGet + symbols packages; test projects and ExampleApp have `IsPackable: false`.

## CI/CD

GitHub Actions workflows in `.github/workflows/`:
- **build.yaml** — restore, build, test, pack on push/PR to master
- **publish.yaml** — on version tags (`v*.*.*`), builds and pushes packages to GitHub Packages

Itenium.Forge.Settings
======================

```sh
dotnet add package Itenium.Forge.Settings
```

## Usage

```cs
using Itenium.Forge.Settings;

var builder = WebApplication.CreateBuilder(args);
var settings = builder.AddForgeSettings<MyAppSettings>();

public class MyAppSettings : IForgeSettings
{
   public ForgeSettings Forge { get; } = new();
   public bool MyProp { get; set; }
}
```

Will load `appsettings.json` and `appsettings.{environment}.json`
with environment:

```cs
string environment = System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
```

Example `appsettings.json`:

```json
{
  "Forge": {
    "ServiceName": "TodoApp.WebApi",
    "TeamName": "Core",
    "Tenant": "itenium",
    "Application": "TodoApp"
  },
  "MyProp": true
}
```

And `appsettings.Development.json`:

```json
{
  "Forge": {
    "Environment": "Development"
  },
  "MyProp": false
}
```

## Local developer overrides

Create `appsettings.Local.json` next to `appsettings.json` to override settings on your machine without affecting shared config files. The file is gitignored and never committed.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mydb;"
  }
}
```

`appsettings.Local.json` takes highest precedence:

```
appsettings.json
  → appsettings.{environment}.json   (shared, committed)
    → appsettings.Local.json         (local only, never committed)
```

**Use this for:** local connection strings, service URLs, feature flags, log levels.
**Do not use this for:** passwords, API keys, or any real credentials — use `dotnet user-secrets` instead.

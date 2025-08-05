Itenium.Forge.Settings
======================

```sh
dotnet package add Itenium.Forge.Settings
```

## Usage

```cs
using Itenium.Forge.Settings;

var settings = ForgeSettings.Load<MyAppSettings>();

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

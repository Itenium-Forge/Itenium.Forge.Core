Itenium.Forge.Logging
======================

```sh
dotnet add package Itenium.Forge.Settings
dotnet add package Itenium.Forge.Logging
```

## Usage

```cs
using Itenium.Forge.Settings;
using Itenium.Forge.Logging;

Log.Logger = LoggingExtensions.CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var settings = builder.AddForgeSettings<MyAppSettings>();
    builder.AddForgeLogging();

    var app = builder.Build();
    app.AddForgeLogging();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
```

Example `appsettings.json` when you want to override the default values:

```json
{
  "Serilog": {
    
  }
}
```

Itenium.Forge.Controllers
=========================

```sh
dotnet add package Itenium.Forge.Controllers
```

## Usage

```cs
using Itenium.Forge.Controllers;

var builder = WebApplication.CreateBuilder(args);
builder.AddControllers();

var app = builder.Build();
app.UseControllers();
```

## Settings

Add to `appsettings.json`

```json
{
  "Hosting": {
    "AllowedHosts": ["*.example.com"],
    "CorsOrigins": ""
  }
}
```

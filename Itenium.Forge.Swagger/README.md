Itenium.Forge.Swagger
=====================

```sh
dotnet add package Itenium.Forge.Swagger
```

## Usage

```cs
using Itenium.Forge.Swagger;

var builder = WebApplication.CreateBuilder(args);
builder.AddSwagger();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}
```

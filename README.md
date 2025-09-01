Itenium.Forge.Core
==================

- Github > Profile > Settings > Developer settings
  - Personal access tokens > Tokens (classic)
  - Generate new token (classic)
    - Scopes: `read:packages`


```sh
dotnet nuget add source --username OWNER --password TOKEN --store-password-in-clear-text --name itenium "https://nuget.pkg.github.com/Itenium-Forge/index.json"
```


## Packages

- [Itenium.Forge.Core](./Itenium.Forge.Core/README.md)
- [Itenium.Forge.Settings](./Itenium.Forge.Settings/README.md)
- [Itenium.Forge.Logging](./Itenium.Forge.Logging/README.md)
- [Itenium.Forge.Swagger](./Itenium.Forge.Swagger/README.md)
- [Itenium.Forge.Controllers](./Itenium.Forge.Controllers/README.md)
- [Itenium.Forge.Security](./Itenium.Forge.Security/README.md)


## TODO

- Template Repository
- Roslynator
  - Warnings as Errors
- Settings:
  - Settings 
  - Secrets in Vault/Consul
- Http:
  - x-correlation-id header forwarding
  - Circuit Breaker



### Database Migrations

```cs
public static void MigrateDb(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
    dbContext.Database.Migrate();
}

/// <summary>
/// For EF Migrations
/// </summary>
internal class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureDbContextBuilder(builder);
        return new AppDbContext(builder.Options);
    }
}
```


### Https Enforcement

Is it enough to just have the reverse proxy take care of this?

```cs
builder.Services.AddHttpsRedirection(options => { options.HttpsPort = 443; });
app.UseHttpsRedirection();
app.UseHsts();
```

### Rate Limiting

```cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 100; // 100 requests/minute
    });
});
```

### Request Validation

We need to have the validation on the frontend and on the backend.
- Just duplicate it?
- Use something like zod and generate something for the backend?
  - Or write in CSharp and generate something for the frontend?
- Have the .NET code call a Node docker that contains the same logic?

Also other features:
- Translated error messages
- List all errors


```cs
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});
```


### Health Checks

```cs
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```


### Others

```cs
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(ttcSettings.PublicImageFolder),
    RequestPath = "/img"
});
```

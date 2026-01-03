Itenium.Forge.Security.OpenIddict
==================================

Self-hosted OpenIddict identity server for Forge applications.

Use this package when you want to run the identity server as part of your .NET application, without external dependencies like Keycloak.

## Installation

```sh
dotnet add package Itenium.Forge.Security.OpenIddict
dotnet add package Microsoft.EntityFrameworkCore.SqlServer  # or your preferred provider
```

## Setup

### 1. Create your DbContext

```cs
using Itenium.Forge.Security.OpenIddict;

public class AppDbContext : ForgeIdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Add your own entities here
    // public DbSet<Product> Products { get; set; }
}
```

### 2. Configure in Program.cs

```cs
using Itenium.Forge.Security;
using Itenium.Forge.Security.OpenIddict;

var builder = WebApplication.CreateBuilder(args);

// Add OpenIddict with your DbContext
builder.AddForgeOpenIddict<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
await app.SeedOpenIddictDataAsync();

// Add security middleware
app.UseForgeSecurity();

app.Run();
```

### 3. Add Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ForgeApp;Trusted_Connection=True;"
  },
  "ForgeConfiguration": {
    "Security": {
      "ClientId": "forge-spa",
      "ClientDisplayName": "Forge SPA",
      "RedirectUris": "http://localhost:3000/callback,http://localhost:5173/callback",
      "AccessTokenLifetimeMinutes": 60,
      "RefreshTokenLifetimeDays": 14
    }
  }
}
```

### 4. Create Migrations

```sh
dotnet ef migrations add InitialIdentity -c AppDbContext
dotnet ef database update -c AppDbContext
```

## Test Users

After seeding, these users are available:

| Username | Password | Roles |
|----------|----------|-------|
| admin | Admin123! | admin, user |
| user | User123! | user |

## Endpoints

| Endpoint | Description |
|----------|-------------|
| POST /connect/token | Get access token (password or refresh_token grant) |
| GET /connect/authorize | Authorization endpoint (for PKCE flow) |
| GET /connect/userinfo | Get current user info |
| POST /connect/logout | Logout |

## Get a Token

```bash
curl -X POST http://localhost:5000/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=forge-spa" \
  -d "username=admin" \
  -d "password=Admin123!" \
  -d "scope=openid profile email roles"
```

## Extending the User Model

Create your own user class:

```cs
public class AppUser : ForgeUser
{
    public string? Department { get; set; }
    public DateTime? HireDate { get; set; }
}
```

Then use it with Identity:

```cs
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
```

Itenium.Forge.Security
======================

TODO: Setup a Keycloak for JWT stuff?
https://github.com/goauthentik/authentik
https://github.com/keycloak/keycloak

TODO: Add current user to logging

```cs
services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = settings.Issuer,
          ValidAudience = settings.Issuer,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSecret))
      };
  });
services.AddAuthorization();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    LogContext.PushProperty("UserName", context.User.Identity?.Name ?? "Anonymous");
    await next();
});
```

TODO: Setup HttpContextProvider

```cs
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<UserProvider>();
```


```sh
dotnet add package Itenium.Forge.Security
```

## Usage

```cs
using Itenium.Forge.Security;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
```

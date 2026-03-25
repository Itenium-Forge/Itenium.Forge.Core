Itenium.Forge.SecurityHeaders
==============================

```sh
dotnet add package Itenium.Forge.SecurityHeaders
```

Adds HTTP security response headers to every response.

## Usage

```cs
using Itenium.Forge.SecurityHeaders;

var app = builder.Build();
app.UseForgeSecurityHeaders();
```

Call `UseForgeSecurityHeaders` early in the pipeline so headers are present on all responses, including errors.

## Default headers

| Header | Default value |
|--------|---------------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |

## Customisation

```cs
app.UseForgeSecurityHeaders(policy => policy
    .AddXFrameOptions("SAMEORIGIN")
    .AddPermissionsPolicy("camera=(), payment=()")
    .RemoveHeader("X-Frame-Options")); // remove a default header
```

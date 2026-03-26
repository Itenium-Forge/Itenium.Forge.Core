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

## Profiles

### `ForApi()` (default)

Strict profile for JSON web APIs. Applied when no configure delegate is passed.

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |

### `ForBrowser()`

For applications that serve HTML (MVC / Razor Pages).

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (HTTPS only) |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'` |

## Customisation

```cs
// Override individual headers
app.UseForgeSecurityHeaders(policy => policy
    .AddXFrameOptions("SAMEORIGIN")
    .AddPermissionsPolicy("camera=(), payment=()")
    .RemoveHeader("X-Frame-Options"));

// Relax CSP for a specific path (e.g. Swagger UI)
app.UseForgeSecurityHeaders(
    policy => policy.ForApi(),
    paths => paths.ForPath("/swagger", p => p
        .ForApi()
        .RemoveHeader("Content-Security-Policy")));
```

## Attribution

The middleware design (pipeline + `HeaderPolicyCollection` + per-header policy classes) is inspired by [NetEscapades.AspNetCore.SecurityHeaders](https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders) by Andrew Lock.
This package is a lighter, dependency-free adaptation with no external NuGet dependencies.

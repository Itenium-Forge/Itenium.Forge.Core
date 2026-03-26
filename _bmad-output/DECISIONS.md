# Decision Log

Architectural decisions made in this repository, recorded in reverse-chronological order.

Each entry follows the ADR format: **Context** (why we faced this choice), **Decision** (what we chose), **Consequences** (trade-offs accepted).

---

## ADR-004 — Explicit authorization policy required; hard crash in Development if omitted

- **Date:** 2026-03-25
- **Status:** Proposed
- **Branch/Story:** A3 — Authorization Policy Enforcement

**Summary:** Forge requires an explicit authorization decision at startup: either `AllowAnonymousByDefault()` or `RequireAuthenticatedByDefault()` with at least one named policy defined. In Development, omitting either crashes the app immediately. In all other environments, a safe fallback (require authenticated) is applied silently. The entire check is flag-based — no reflection, no endpoint scanning.

### Context

After `AddForgeKeycloak()` or `AddForgeOpenIddict()` is called, ASP.NET Core's authorization pipeline is active but has no fallback policy. Any endpoint without an explicit `[Authorize]` attribute is publicly accessible by default. Nothing in the current Forge startup contract forces the developer to make a conscious choice about this. A developer can wire up the full security stack and still ship a wide-open API because they forgot a single configuration call.

Additionally, calling `RequireAuthenticatedByDefault()` without defining any named authorization policies is also a half-finished configuration — endpoints decorated with `[Authorize(Policy = "admin")]` will throw at runtime if the policy was never registered.

Enforcing this with reflection (scanning all registered controllers/routes to verify every endpoint is covered) would be expensive at startup and couple the check to the MVC route discovery phase — increasing startup time for a mistake that should have been caught during development.

### Decision

Make the authorization configuration an **explicit, required step**. Exactly one of the following must be called:

```csharp
// Option A — public API, no authentication required
builder.AddForgeKeycloak(auth => auth.AllowAnonymousByDefault());

// Option B — require authentication; at least one named policy must also be defined
builder.AddForgeKeycloak(auth => auth
    .RequireAuthenticatedByDefault()
    .AddPolicy("admin", policy => policy.RequireRole("admin"))
    .AddPolicy("user",  policy => policy.RequireRole("user")));
```

Both paths flip a flag (`ForgeAuthorizationOptions.IsConfigured = true`) on an options object registered in DI. `RequireAuthenticatedByDefault()` additionally requires that at least one named policy is registered — validated via the same flag mechanism, not reflection.

#### Enforcement — Development (primary design intent)

`UseForgeSecurity()` throws `InvalidOperationException` if:
- Neither `AllowAnonymousByDefault()` nor `RequireAuthenticatedByDefault()` was called, **or**
- `RequireAuthenticatedByDefault()` was called but no named policies were defined.

```
InvalidOperationException: No authorization policy configured.
Call RequireAuthenticatedByDefault() (with at least one named policy) or AllowAnonymousByDefault().
```

The app does not start. This turns a latent security hole into an immediate, unmissable failure the moment the developer runs the app locally.

#### Enforcement — all other environments (safety net)

If no explicit configuration was made, `UseForgeSecurity()` silently applies `RequireAuthenticatedByDefault()` as a fallback and emits a startup warning:

```
[WARN] Forge: No authorization policy configured. Defaulting to RequireAuthenticatedByDefault.
       Call RequireAuthenticatedByDefault() or AllowAnonymousByDefault() on the security builder.
```

Endpoints fail closed rather than open.

#### `AllowAnonymousByDefault()` in non-Development environments

`AllowAnonymousByDefault()` is valid in all environments — legitimate public APIs exist. When called outside Development, Forge emits a startup warning:

```
[WARN] Forge: Authorization fallback policy is AllowAnonymous.
       All endpoints are publicly accessible unless individually decorated with [Authorize].
```

### Consequences

- Developers are forced to make a conscious, complete authorization decision at wiring time; mistakes surface in seconds locally, not in a security review.
- `RequireAuthenticatedByDefault()` without policies is caught at startup, not at the first `[Authorize(Policy = "...")]` call at runtime.
- No reflection, no MVC route scanning — startup overhead is negligible.
- `UseForgeSecurity()` gains a dependency on `IWebHostEnvironment` to read the current environment.
- CI pipelines typically run in a non-Development environment and receive the silent fallback rather than a hard crash — intentional; the crash is a developer feedback tool, not a CI gate.
- Both `AddForgeKeycloak` and `AddForgeOpenIddict` expose the same builder API; the flag and validation logic lives once in `Itenium.Forge.Security`.

---

## ADR-003 — Use `System.Diagnostics.Activity` in Logging without an OTel package dependency

- **Date:** 2026-03-24
- **Status:** Accepted
- **Branch/Story:** C1 — OpenTelemetry

### Context

`Itenium.Forge.Logging` needs to stamp every Serilog log entry with the current `TraceId` and `SpanId` so logs can be correlated with traces in Grafana. The obvious approach is to reference the OpenTelemetry SDK. However, `Itenium.Forge.Logging` is a stable NuGet package, and adding an OTel dependency would pull in the entire OTel SDK chain.

### Decision

Use `System.Diagnostics.Activity` (built into .NET — no NuGet package required) inside `ActivityEnricher`. The OTel SDK hooks into `Activity` internally; it does not own the type. Reading `Activity.Current.TraceId` works regardless of whether `Itenium.Forge.Telemetry` is installed.

### Consequences

- `Itenium.Forge.Logging` has **zero OTel package dependencies**.
- `CorrelationIdMiddleware` starts its own fallback `Activity` per request when `Activity.Current` is null (i.e. when `Itenium.Forge.Telemetry` is not installed). It reads the incoming `traceparent` header to continue an existing trace, or generates a fresh W3C trace ID when absent.
- Log enrichment with `TraceId`/`SpanId` works in both scenarios — with or without `Itenium.Forge.Telemetry`.
- Outgoing `HttpClient` calls (via `IHttpClientFactory`) forward the `traceparent` header through `TraceparentHandler`, which is registered automatically by `AddForgeLogging()`. When `Itenium.Forge.Telemetry` is installed, the OTel SDK overwrites this header with a correctly-scoped child span; when it is absent, `TraceparentHandler` alone provides the propagation.
- `HttpClient` instances created manually with `new HttpClient()` do **not** get `TraceparentHandler` — only factory-managed clients.
- No extra NuGet weight for teams that use Serilog without OTel.

---

## ADR-002 — Separate `Itenium.Forge.Telemetry` package for OpenTelemetry

- **Date:** 2026-03-24
- **Status:** Accepted
- **Branch/Story:** C1 — OpenTelemetry

### Context

`OpenTelemetry.Exporter.Prometheus.AspNetCore` has **never shipped a stable NuGet release** — all 28+ published versions are pre-release (latest: `1.15.0-beta.1`). NuGet rule `NU5104` prevents a stable package from declaring a pre-release dependency. With `TreatWarningsAsErrors: true` in the build, this surfaces as a hard build failure. Folding OTel into `Itenium.Forge.Logging` would permanently block it from being a stable package.

### Decision

Create a separate `Itenium.Forge.Telemetry` package that owns all OTel references and suppresses `NU5104` locally. `Itenium.Forge.Logging` keeps only Serilog dependencies and remains fully stable.

### Consequences

- `Itenium.Forge.Logging` remains a **stable, production-grade package**.
- Teams that don't need Prometheus metrics can skip `Itenium.Forge.Telemetry` entirely.
- When `Prometheus.AspNetCore` eventually ships a stable release, the `<NoWarn>` can be removed with no changes to Logging.
- Consuming apps must reference both packages and call both `AddForgeLogging()` + `AddForgeTelemetry()`.

---

## ADR-001 — Use W3C `traceparent` instead of custom `x-correlation-id`

- **Date:** 2026-03-24
- **Status:** Accepted
- **Branch/Story:** C1 — OpenTelemetry

### Context

`CorrelationIdMiddleware` (implemented in story A1) originally used a custom `x-correlation-id` request/response header to propagate a request-scoped trace identifier. When OpenTelemetry was added, OTel generates its own 128-bit trace ID per request and propagates it via the W3C `traceparent` header. Having two parallel identifiers for the same concept causes confusion.

The `traceparent` format is:
```
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
              ^  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ ^^^^^^^^^^^^^^^^ ^^
              version  traceId (32 hex chars)      spanId           flags
```

### Decision

Switch `CorrelationIdMiddleware` to read from `Activity.Current.TraceId` (the OTel trace ID, set from the incoming `traceparent` header by the OTel ASP.NET Core instrumentation) rather than from `x-correlation-id`. This single ID is propagated to:

- `HttpContext.TraceIdentifier` — picked up by ProblemDetails `Extensions["traceId"]`
- Serilog `LogContext` — every log line carries `TraceId`
- All outgoing `HttpClient` calls — automatically injected by the OTel SDK

### Consequences

- One ID for one request, visible in logs, traces (Grafana Tempo), and error responses.
- The `x-correlation-id` **request** header is no longer the authoritative source; callers should send `traceparent` to propagate an existing trace.
- The `CorrelationIdMiddleware.HeaderName` constant is updated to `"traceparent"` to reflect this.
- Tests that previously verified `x-correlation-id` round-tripping were updated to verify `traceparent` propagation and OTel trace ID format (32 lowercase hex chars).

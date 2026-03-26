# Decision Log

Architectural decisions made in this repository, recorded in reverse-chronological order.

Each entry follows the ADR format: **Context** (why we faced this choice), **Decision** (what we chose), **Consequences** (trade-offs accepted).

---

## ADR-005 — Apply security headers directly, not via `Response.OnStarting`

- **Date:** 2026-03-24
- **Status:** Accepted
- **Branch/Story:** A2 — Security Headers

### Context

`NetEscapades.AspNetCore.SecurityHeaders` (the reference implementation) applies headers inside a `Response.OnStarting` callback so they are written at the last possible moment before the response is sent. This protects against downstream middleware overwriting them. However, `Response.OnStarting` callbacks are not fired by `DefaultHttpContext` in unit tests — only by the Kestrel pipeline — making unit tests impossible without a full integration test setup.

### Decision

Apply headers directly at the start of `SecurityHeadersMiddleware.Invoke`, before calling `_next`. `UseForgeSecurityHeaders` is registered early in the pipeline (before routing, controllers, and error handling), so there is no realistic downstream middleware that would remove security headers.

### Consequences

- All unit tests can use `DefaultHttpContext` without a running Kestrel server.
- Theoretically, a middleware registered after `UseForgeSecurityHeaders` could overwrite headers. In practice, no Forge middleware does this, and the position in the pipeline prevents it.

---

## ADR-004 — Source-copy `NetEscapades.AspNetCore.SecurityHeaders` instead of NuGet dependency; exclude CSP from defaults

- **Date:** 2026-03-24
- **Status:** Accepted
- **Branch/Story:** A2 — Security Headers

### Context

`NetEscapades.AspNetCore.SecurityHeaders` is the standard library for security headers in ASP.NET Core. Taking it as a NuGet dependency would mean the Forge package exposes a transitive dependency on a third-party library for what is essentially a small amount of header-writing logic. Additionally, `Content-Security-Policy` — the most complex header the library supports — breaks Swagger UI in development because Swagger uses inline scripts and styles that a strict CSP would block.

### Decision

Copy the core design (middleware + `HeaderPolicyCollection` + per-header policy classes) from [NetEscapades.AspNetCore.SecurityHeaders](https://github.com/andrewlock/NetEscapades.AspNetCore.SecurityHeaders) into `Itenium.Forge.SecurityHeaders` with no external NuGet dependencies. The default policy (`AddApiDefaults`) includes the headers meaningful for a JSON web API — `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`, `Referrer-Policy`, `Permissions-Policy` — and deliberately omits `Content-Security-Policy`.

### Consequences

- `Itenium.Forge.SecurityHeaders` has **zero external NuGet dependencies**.
- Teams that need CSP can add it manually via `UseForgeSecurityHeaders(p => p.AddContentSecurityPolicy(...))` once they implement the policy class, or use NetEscapades directly alongside.
- Swagger UI works out of the box in development with no extra configuration.
- The implementation is intentionally simpler than NetEscapades (no nonce support, no endpoint-specific policies) — sufficient for the API use case.

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

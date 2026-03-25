# Decision Log

## ADR-005 — Request logging field masking: JSON body + query string, recursive, case-insensitive

- **Date:** 2026-03-25
- **Status:** Proposed
- **Branch/Story:** A9 — Request/response logging field masking

**Summary:** Sensitive fields in JSON request bodies and query-string parameters are replaced with `***` before logging. Masking is recursive (deep), case-insensitive, configurable, and applied exclusively in the logging middleware.

### Context

`RequestLoggingMiddleware` logs the full request body and query string. Without masking, secrets such as passwords or tokens appear in plain text in Serilog sinks (file, Loki). The implementation must decide:

1. **Which parts of the request to mask** — body only, or also query string?
2. **How deeply to traverse** — top-level keys only, or recursive into nested objects and arrays?
3. **How to handle non-JSON bodies** — redact entirely or pass through unchanged?
4. **Where in the stack to apply masking** — middleware, model layer, or Serilog destructure policy?
5. **How custom field lists interact with the defaults** — extend, or replace entirely?

### What is masked and what is not

| Target | Masked? | Reason |
|--------|:-------:|--------|
| JSON request body — matching field values | Yes | Primary attack surface |
| Query-string — matching parameter values | Yes | Low-cost, same risk profile |
| Nested / array fields in JSON body | Yes | Shallow masking is trivially bypassed |
| Response body | No | Not currently logged; out of scope |
| HTTP headers (e.g. `Authorization`) | No | Headers are not logged by the middleware |
| Non-JSON request body (form data, binary) | No | Returned as-is; no reliable key extraction |

#### Default masked fields (case-insensitive)

| Field name | Typical use |
|------------|-------------|
| `password` | Login / register payloads |
| `passwd` | Legacy / Unix-style naming |
| `token` | Generic token field |
| `secret` | Client secrets, shared secrets |
| `authorization` | Auth header mirrored in body |
| `client_secret` | OAuth2 client credentials |
| `api_key` | API key fields |
| `access_token` | OAuth2 access tokens |
| `refresh_token` | OAuth2 refresh tokens |

### Decisions

#### 1. Custom `MaskedFields` replaces the defaults entirely

When a caller calls `options.SetFields(...)` the default set is discarded and replaced. `options.AddFields(...)` extends the defaults. This gives full control without surprising interactions.

**Why:** An opt-in extension (`AddFields`) is the common case; a full replacement (`SetFields`) is provided for teams that want a narrower or entirely different list.

#### 2. Non-JSON bodies are passed through unchanged

If the body cannot be parsed as a `JsonNode`, the raw string is logged as-is with no redaction.

**Why:** Form-encoded and multipart bodies have no reliable key structure. Redacting the entire body would lose diagnostic value for the majority of non-sensitive payloads. Teams with sensitive form fields should switch to JSON or handle masking at the model layer.

#### 3. Masking is recursive (deep traversal)

Both JSON objects and JSON arrays are traversed. Any string-valued leaf whose property name matches a masked field name is replaced with `"***"`.

**Why:** Shallow masking is trivially bypassed by nesting a `password` field one level deeper (e.g. inside an `auth` wrapper object). Deep masking is the only safe default.

#### 4. Masking is applied in `RequestLoggingMiddleware`, not at the model layer

The masking runs on the raw body string just before the log call.

**Why:** Applying it at the model layer (custom `JsonConverter`, `IModelBinder`) would mask values in application memory, making debugging harder. Middleware masking is logging-only and does not affect business logic.

#### 5. Query-string parameter values are also masked

The query-string dictionary is traversed; values for matching keys are replaced with `***`.

**Why:** Tokens and API keys are sometimes passed as query parameters (e.g. `?api_key=...`). The cost is negligible and the risk profile is identical to the JSON body.

### Consequences

- `FieldMaskingOptions` is registered as a singleton; callers configure it via `AddForgeLogging(options => ...)`.
- `FieldMasker` is an `internal static` helper — no public API surface.
- If the JSON body cannot be parsed (invalid JSON), it is logged as-is. No silent data loss.
- Response body masking is out of scope. If needed in future, it should be its own feature.
- Header masking is out of scope. Teams that log headers should use Serilog destructure policies.

---

Architectural decisions made in this repository, recorded in reverse-chronological order.

Each entry follows the ADR format: **Context** (why we faced this choice), **Decision** (what we chose), **Consequences** (trade-offs accepted).

---

## ADR-004 — Local developer overrides use `appsettings.Local.json`, never machine name

- **Date:** 2026-03-25
- **Status:** Accepted
- **Branch/Story:** A8 — Local developer appsettings

### Context

Developers need to override settings locally without affecting shared config files. `appsettings.{MachineName}.json` was rejected: filenames are unpredictable, the gitignore pattern must cover every machine name, and it breaks in containers and CI. ASP.NET Core user secrets are for credentials only — not suitable for non-secret local overrides like base URLs or feature flags.

### Decision

Load `appsettings.Local.json` as the final, highest-precedence configuration layer — gitignored and never committed.

```
appsettings.json
  → appsettings.{environment}.json   (optional — shared, committed)
    → appsettings.Local.json         (optional — local only, never committed)
```

### Consequences

- One predictable filename; a single `.gitignore` entry covers all workstations.
- CI/CD pipelines never have the file — they run on environment-specific config only.
- Secrets do not belong here; use `dotnet user-secrets` or a secrets manager for credentials.

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

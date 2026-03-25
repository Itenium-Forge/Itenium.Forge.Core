# Decision Log

Architectural decisions made in this repository, recorded in reverse-chronological order.

Each entry follows the ADR format: **Context** (why we faced this choice), **Decision** (what we chose), **Consequences** (trade-offs accepted).

---

## ADR-009 — Local developer overrides use `appsettings.Local.json`, never machine name

- **Date:** 2026-03-25
- **Status:** Proposed
- **Branch/Story:** A8 — Local developer appsettings

**Summary:** A gitignored `appsettings.Local.json` file is the sanctioned way for developers to override settings locally. Machine-name files are not used. Secrets must not be stored in this file.

### Context

Developers need to override settings locally without affecting shared configuration files — different database connection strings, local service URLs, toggled feature flags. Three approaches were considered:

#### Option A — `appsettings.{MachineName}.json`

- Each developer gets a file named after their machine (e.g. `appsettings.Laptop_GC.json`)
- File names are not predictable — new team members must discover the convention from documentation or a colleague
- The gitignore pattern must cover every possible machine name, or each developer adds their own file to gitignore manually
- Does not work well in containers or CI where machine name is auto-generated

#### Option B — `appsettings.Local.json` (chosen)

- One predictable, well-known filename the entire team uses
- A single gitignore entry covers all workstations
- Matches established conventions in adjacent ecosystems (Next.js `.env.local`, Vite `.env.local`)
- Easy to document: "create `appsettings.Local.json` next to `appsettings.json`"

#### Option C — ASP.NET Core user secrets (`dotnet user-secrets`)

- Designed specifically for secrets; values are stored outside the repo in `%APPDATA%\Microsoft\UserSecrets`
- Not suitable for non-secret local overrides (base URLs, feature flags, log levels)
- Per-project setup required; harder to share partial configs with the team
- Complementary to Option B, not a replacement

### Decision

Load `appsettings.Local.json` as the final, highest-precedence configuration layer. The full loading order is:

```
appsettings.json
  → appsettings.{environment}.json   (optional — shared, committed)
    → appsettings.Local.json         (optional — local only, never committed)
```

`appsettings.Local.json` is always optional. When absent the application starts normally.

**Convention — what belongs in `appsettings.Local.json`:**
- Local service URLs, ports, connection strings pointing to a local instance
- Feature flags or log levels for local debugging
- Non-sensitive keys (e.g. a client ID for a shared dev tenant)

**Convention — what does NOT belong in `appsettings.Local.json`:**
- Passwords, API keys, certificates, tokens, or any credential with real access
- Use ASP.NET Core user secrets (`dotnet user-secrets`) or a secrets manager for those

### Consequences

- `appsettings.Local.json` is added to `.gitignore` at the repo root — it can never be accidentally committed.
- Every developer creates their own `appsettings.Local.json`; the file is never tracked.
- CI/CD pipelines do not have the file and run on the environment-specific config only — intentional.
- A template `appsettings.Local.json.example` can be committed to the repo to document available overrides.

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

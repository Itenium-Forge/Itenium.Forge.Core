# Decision Log

Architectural decisions made in this repository, recorded in reverse-chronological order.

Each entry follows the ADR format: **Context** (why we faced this choice), **Decision** (what we chose), **Consequences** (trade-offs accepted).

---

## ADR-008 — Pagination uses 1-based page numbers and page number + page size parameters

- **Date:** 2026-03-25
- **Status:** Proposed
- **Branch/Story:** A6 — `IForgePagedResult<T>` + pagination contracts

**Summary:** `ForgePageQuery` exposes `Page` (1-based) and `PageSize` rather than `Offset` and `Limit`. Page numbers start at 1, not 0.

### Context

Two independent choices must be made when designing a pagination API contract:

1. **How to express position:** page number + page size vs. raw offset + limit
2. **Where page numbering starts:** 1-based vs. 0-based

Both are pure convention choices with no technical winner. They must be settled before any consuming code (B14 EF Core extension, ForgeBuilder UI blocks, API controllers) is written — changing the contract later is a breaking change for all consumers.

#### Option A — Page number + page size (`?page=2&pageSize=20`)

- Consumer thinks in pages: "give me page 2"
- Maps naturally to UI pagination controls that show "Page 2 of 10"
- `TotalPages`, `HasPreviousPage`, `HasNextPage` on the result type read intuitively
- Offset is an internal concern: `(page - 1) * pageSize` — consumers never see it
- Standard in most REST APIs targeting human-facing UIs (ASP.NET Core, Spring, Django REST)

#### Option B — Offset + limit (`?offset=20&limit=20`)

- Consumer thinks in rows: "skip 20 rows, give me 20"
- More flexible — can resume from an arbitrary row
- Common in APIs designed for programmatic consumers or streaming exports (GitHub API, many SQL-style REST APIs)
- `TotalPages` becomes awkward to compute client-side; `HasNextPage` requires knowing total count

#### Option C — 0-based page numbers (`?page=0&pageSize=20`)

- Consistent with array/list indexing in most languages
- `Skip = page * pageSize` is marginally simpler than `(page - 1) * pageSize`
- Confusing in UI contexts: "Page 0 of 9" is never what a user sees
- `HasPreviousPage => Page > 0` is technically correct but reads oddly

#### Option D — 1-based page numbers (`?page=1&pageSize=20`)

- Matches how users and UI controls express pages
- `HasPreviousPage => Page > 1` reads naturally
- `ForgePageQuery` default of `Page = 1` is unambiguous — there is always a first page
- Off-by-one risk is isolated to the single internal `(page - 1) * pageSize` calculation

### Decision

Use **page number + page size** (Option A) with **1-based page numbers** (Option D).

```csharp
// Query parameters
GET /api/resources?page=2&pageSize=20

// Result contract
{
  "items": [...],
  "page": 2,
  "pageSize": 20,
  "totalCount": 57,
  "totalPages": 3,
  "hasPreviousPage": true,
  "hasNextPage": true
}
```

`ForgePageQuery` defaults: `Page = 1`, `PageSize = 20`.

### Consequences

- UI components receive `page`, `totalPages`, `hasPreviousPage`, `hasNextPage` — no client-side arithmetic needed.
- The internal offset calculation `(page - 1) * pageSize` lives in one place: `IQueryable<T>.ToForgePagedResultAsync()` (B14).
- Offset/limit-style access (e.g. streaming exports) is not supported by this contract; a separate `ForgeScrollQuery` can be introduced when a concrete use case arises.
- `page=0` submitted by a client is an invalid request; consumers must validate and return 400. A `PageSize` maximum cap is not enforced by the contract — individual controllers or a future Forge filter can add this.

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

# Forge Progress

🟢 Complete &nbsp;·&nbsp; ⭕ Not Started

**Effort:** S = Small (days) · M = Medium (1–2 weeks) · L = Large (2–4 weeks) · XL = Extra Large (multi-sprint)

**Value:** 1 = nice-to-have · 5 = critical / blocking

**Risk:** Lo · Med · Hi (probability of scope creep, design churn, or wrong abstraction)

---

| Code | Feature | Effort | Value | Risk | Status | Dependencies |
|------|---------|:------:|:-----:|:----:|:------:|:-------------|
| **Foundation** | | | | | | |
| — | Settings & Configuration | S | 5 | Lo | 🟢 | — |
| — | Logging — Serilog, request logging, Loki | M | 5 | Lo | 🟢 | Settings |
| — | Controllers — MVC, JSON, CORS, host filtering | S | 5 | Lo | 🟢 | Settings |
| — | Swagger / OpenAPI | S | 4 | Lo | 🟢 | Controllers |
| — | Health Checks — liveness & readiness probes | S | 4 | Lo | 🟢 | Settings |
| — | Security Core — ICurrentUser, claims extraction, authorization policy | S | 5 | Med | 🟢 | Settings |
| — | Security — Keycloak JWT Bearer | S | 5 | Med | 🟢 | Security Core |
| — | Security — OpenIddict identity server | L | 5 | Hi | 🟢 | Security Core |
| **Phase A — Chassis Foundation** | | | | | | |
| A1 | Correlation ID — W3C traceparent propagation | S | 5 | Lo | 🟢 | Logging |
| A2 | Startup config validation (`ValidateOnStart()`) | S | 4 | Lo | ⭕ | Settings |
| A3 | Security Headers middleware | S | 4 | Lo | 🟢 | — |
| A4 | API versioning (`Asp.Versioning.Mvc`) | S | 4 | Lo | ⭕ | Controllers, Swagger |
| A5 | Rate limiting (`Microsoft.AspNetCore.RateLimiting`) | M | 4 | Lo | ⭕ | A1 |
| A6 | `IForgePagedResult<T>` + pagination contracts | S | 5 | Med | ⭕ | — |
| A7 | Response compression | S | 2 | Lo | ⭕ | Controllers |
| A8 | Local developer overrides (`appsettings.Local.json`) | S | 3 | Lo | 🟢 | Settings |
| A9 | Request/response logging field masking | S | 3 | Lo | ⭕ | Logging |
| **Phase B — Data & Messaging** | | | | | | |
| B1 | DbContext registration helper | M | 5 | Med | ⭕ | A2 |
| B2 | EF audit interceptor (`IAuditable`, `AuditInfo`) | M | 5 | Med | ⭕ | B1, Security Core |
| B3 | Soft delete interceptor (`ISoftDeletable`) | S | 4 | Lo | ⭕ | B1 |
| B4 | Full row-change audit logging (diff-only) | L | 4 | Hi | ⭕ | B1, B2 |
| B5 | Auto-migration on startup (dev) | S | 3 | Lo | ⭕ | B1 |
| B6 | Connection string management conventions | S | 3 | Lo | ⭕ | B1, Settings |
| B7 | HttpClientFactory with Forge defaults | M | 5 | Lo | ⭕ | A1 |
| B8 | Resilience pipeline for HttpClient (Polly v8) | M | 4 | Lo | ⭕ | B7 |
| B9 | MassTransit integration | L | 4 | Med | ⭕ | A1, B1 |
| B10 | Outbox pattern (MassTransit EF outbox) | M | 4 | Med | ⭕ | B1, B9 |
| B11 | Distributed cache setup (Redis) | M | 3 | Lo | ⭕ | — |
| B12 | Background services + Quartz scheduling | M | 3 | Lo | ⭕ | A1 |
| B13 | `IForgeContext` (AsyncLocal: correlation, user, tenant, trace) | M | 4 | Med | ⭕ | A1, Security Core |
| B14 | `IQueryable<T>` paging/sorting extension methods | S | 3 | Lo | ⭕ | A6, B1 |
| B15 | Request validation (FluentValidation + ProblemDetails) | M | 4 | Med | ⭕ | Controllers |
| **Phase C — Observability & Operations** | | | | | | |
| C1 | OpenTelemetry tracing (OTLP export) | M | 5 | Lo | 🟢 | Logging |
| C2 | OpenTelemetry metrics (Prometheus) | M | 4 | Lo | 🟢 | C1 |
| C3 | Health check enrichment (auto-register, build info) | S | 4 | Lo | ⭕ | Health Checks, B1 |
| C4 | Grafana dashboard templates (JSON) | M | 3 | Lo | ⭕ | C1, C2 |
| C5 | Prometheus alerting rules | S | 3 | Lo | ⭕ | C2 |
| C6 | Vault/Consul secrets integration | L | 4 | Med | ⭕ | Settings |
| C7 | Runtime log level control (Serilog `LoggingLevelSwitch`) | S | 3 | Lo | ⭕ | Logging |
| C8 | Config debug endpoint (dev-only, secrets masked) | S | 2 | Lo | ⭕ | Settings |
| C9 | Config gap detection (startup warnings) | M | 3 | Med | ⭕ | — |
| **Phase D — Developer Experience** | | | | | | |
| D1 | EditorConfig + Roslyn analyzers | M | 4 | Lo | ⭕ | — |
| D2 | `ForgeWebApplicationFactory` (test helper) | M | 5 | Lo | ⭕ | B1, Security Core |
| D3 | Authenticated HttpClient test helper | S | 4 | Lo | ⭕ | D2, Security Core |
| D4 | Test data builders (Forge types) | S | 2 | Lo | ⭕ | D2 |
| D5 | MassTransit test harness auto-config | S | 3 | Lo | ⭕ | D2, B9 |
| D6 | Local dev docker-compose (full stack) | L | 4 | Med | ⭕ | C1, C4, B9 |
| D7 | Template repository (`dotnet new forge-api`) | L | 5 | Med | ⭕ | D6 |
| D8 | Feature flags (`Microsoft.FeatureManagement`) | S | 3 | Lo | ⭕ | Settings |
| D9 | Localization (`AddForgeLocalization()`) | M | 3 | Med | ⭕ | Controllers |
| D10 | Reusable CI/CD workflow (GitHub Actions) | M | 4 | Lo | 🟢 | — |
| D11 | Base Dockerfile | S | 3 | Lo | ⭕ | D7 |
| **Phase E — Runtime Layer** | | | | | | |
| E1 | Metadata API (compile-time + runtime, single source of truth) | XL | 5 | Hi | ⭕ | B1, B2, A6 |
| E2 | Auto-CRUD / generic controller | XL | 5 | Hi | ⭕ | E1, B1, B2, B3, A6 |
| E3 | Field-level permissions (visible/hidden/readonly per role) | L | 4 | Med | ⭕ | E1, Security Core |
| E4 | Command sourcing (CQRS command log) | L | 3 | Hi | ⭕ | B1 |
| E5 | TypeScript client generation (orval / openapi-typescript) | L | 4 | Med | ⭕ | Swagger, E1 |
| E6 | SignalR real-time entity broadcasts | M | 4 | Med | ⭕ | E2 |
| E7 | Optimistic concurrency / conflict resolution | M | 4 | Med | ⭕ | B1, E2 |
| E8 | Template project structure (Api + Contracts + Services + Tests) | M | 3 | Lo | ⭕ | D7 |
| **Phase F — ForgeBuilder Platform** | | | | | | |
| F1 | ForgePencil design system (shadcn/ui + tokens) | L | 5 | Med | ⭕ | — |
| F2 | Storybook for ForgePencil | M | 4 | Lo | ⭕ | F1 |
| F3 | Admin wizard (branding, languages, auth config) | L | 4 | Med | ⭕ | F1, E1 |
| F4 | UI builder (grid-based form/page designer) | XL | 5 | Hi | ⭕ | F1, F3, E1, E2 |
| F5 | Block system (table, detail, form, dashboard) | XL | 5 | Hi | ⭕ | F4, E2, E6 |
| F6 | Config file structure (JSON/YAML, Git output) | L | 5 | Hi | ⭕ | F3, F4 |
| F7 | Config interpreter (frontend runtime reads config) | XL | 5 | Hi | ⭕ | F5, F6 |
| F8 | Eject to plain React codebase | L | 4 | Hi | ⭕ | F7 |
| F9 | Dark mode (token validation) | S | 3 | Lo | ⭕ | F1 |
| F10 | Layout system (table/cards/calendar/kanban/map per entity) | L | 4 | Med | ⭕ | F5 |

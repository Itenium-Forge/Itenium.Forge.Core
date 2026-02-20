# Itenium.Forge -- Solution Matrix

**Date:** 2026-02-20
**Source:** Brainstorming session 2026-02-19/20

## Scoring Legend

| Dimension | Scale | Meaning |
|-----------|-------|---------|
| **Effort** | S / M / L / XL | Days / 1-2 weeks / 2-4 weeks / Multi-sprint |
| **Value** | 1-5 | 1 = nice-to-have, 5 = critical/blocking |
| **Risk** | Lo / Med / Hi | Probability of scope creep, design churn, or wrong abstraction |

## Dependency Notation

- `→` means "must come before"
- Items in the same phase have no ordering constraint unless noted
- Cross-phase dependencies are called out explicitly

---

## Phase A: Chassis Foundation

> **Goal:** Fill the quick-win gaps. Every item here is small effort, high value, and unblocks downstream work.
> **Estimated total:** 2-3 weeks

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| A1 | Correlation ID middleware | S | 5 | Lo | None (half-done in Serilog enricher) | Logging (extend) |
| A2 | Startup config validation (`ValidateOnStart()`) | S | 4 | Lo | Settings | Settings (extend) |
| A3 | Security headers middleware | S | 4 | Lo | None | Security (extend) |
| A4 | API versioning (`Asp.Versioning.Mvc`) | S | 4 | Lo | Controllers, Swagger | **New: Itenium.Forge.ApiVersioning** or fold into Controllers |
| A5 | Rate limiting (`Microsoft.AspNetCore.RateLimiting`) | M | 4 | Lo | None, but benefits from A1 for logging | **New: Itenium.Forge.RateLimiting** or fold into Security |
| A6 | `IForgePagedResult<T>` + pagination contracts | S | 5 | Med | None | Core (extend) |
| A7 | Response compression | S | 2 | Lo | None | Controllers (extend) |
| A8 | Environment-specific overrides (`appsettings.{MachineName}.json`) | S | 3 | Lo | Settings | Settings (extend) |
| A9 | Request/response logging field masking | S | 3 | Lo | Logging | Logging (extend) |

**Phase A critical path:** A6 (get `IForgePagedResult<T>` shape right early -- Black Hat risk)

### Phase A packaging decisions needed

- A4: New package or extend Controllers? Recommend extending Controllers + Swagger together.
- A5: New package or fold into Security? Recommend new package -- rate limiting is independent of auth.

---

## Phase B: Data & Messaging

> **Goal:** The heavy infrastructure layer. EF Core integration, MassTransit, caching, background services.
> **Estimated total:** 4-6 weeks
> **Hard dependency:** A1 (correlation ID), A6 (paged result)

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| B1 | DbContext registration helper | M | 5 | Med | A2 (config validation) | **New: Itenium.Forge.Data** |
| B2 | EF audit interceptor (`IAuditable`, `AuditInfo`) | M | 5 | Med | B1, Security (ICurrentUser) | Data |
| B3 | Soft delete interceptor (`ISoftDeletable`) | S | 4 | Lo | B1 | Data |
| B4 | Full row-change audit logging (diff-only) | L | 4 | Hi | B1, B2 | Data (or separate Data.Auditing) |
| B5 | Auto-migration on startup (dev) | S | 3 | Lo | B1 | Data |
| B6 | Connection string management conventions | S | 3 | Lo | B1, Settings | Data |
| B7 | HttpClientFactory with Forge defaults | M | 5 | Lo | A1 (correlation ID forwarding) | **New: Itenium.Forge.HttpClient** |
| B8 | Resilience pipeline for HttpClient (Polly v8) | M | 4 | Lo | B7 | HttpClient |
| B9 | MassTransit integration | L | 4 | Med | A1, B1 (for outbox) | **New: Itenium.Forge.MassTransit** |
| B10 | Outbox pattern (MassTransit EF outbox) | M | 4 | Med | B1, B9 | MassTransit |
| B11 | Distributed cache setup (Redis) | M | 3 | Lo | None | **New: Itenium.Forge.Caching** |
| B12 | Background services + Quartz scheduling | M | 3 | Lo | A1 | **New: Itenium.Forge.BackgroundServices** |
| B13 | `IForgeContext` (AsyncLocal: correlation, user, tenant, trace) | M | 4 | Med | A1, Security | Core (extend) or **New: Itenium.Forge.Context** |
| B14 | `IQueryable<T>` paging/sorting extension methods | S | 3 | Lo | A6, B1 | Data |
| B15 | Request validation (FluentValidation + ProblemDetails) | M | 4 | Med | Controllers | Controllers (extend) or **New: Itenium.Forge.Validation** |

**Phase B critical path:** B1 → B2 → B4 (data foundation), B7 → B8 (HTTP resilience), B9 → B10 (messaging)

### Phase B packaging decisions needed

- B13: Extend Core or new package? `IForgeContext` is cross-cutting. Recommend Core since it has no heavy deps.
- B15: FluentValidation vs DataAnnotations vs something else. FluentValidation is Apache 2.0, good fit.

---

## Phase C: Observability & Operations

> **Goal:** Complete the observability story. Production readiness.
> **Estimated total:** 3-4 weeks
> **Can run in parallel with Phase D.**

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| C1 | OpenTelemetry tracing (OTLP export) | M | 5 | Lo | Logging (code already commented-out) | Logging (extend) or **New: Itenium.Forge.Observability** |
| C2 | OpenTelemetry metrics (Prometheus) | M | 4 | Lo | C1 (shared OTLP config) | Same as C1 |
| C3 | Health check enrichment (auto-register, build info) | S | 4 | Lo | HealthChecks, B1/B9/B11 (auto-discover) | HealthChecks (extend) |
| C4 | Grafana dashboard templates (JSON) | M | 3 | Lo | C1, C2 | Ships with docker-compose |
| C5 | Prometheus alerting rules | S | 3 | Lo | C2 | Ships with docker-compose |
| C6 | Vault/Consul secrets integration | L | 4 | Med | Settings | **New: Itenium.Forge.Vault** |
| C7 | Runtime log level control (Serilog `LoggingLevelSwitch`) | S | 3 | Lo | Logging | Logging (extend) |
| C8 | Config debug endpoint (dev-only, secrets masked) | S | 2 | Lo | Settings | Settings (extend) |
| C9 | Config gap detection (startup warnings) | M | 3 | Med | All Forge packages | Cross-cutting, in each package |

**Phase C packaging decision:** Rename Logging to Observability? Or keep Logging and add tracing/metrics there? Recommend keeping `Itenium.Forge.Logging` and folding OTEL in -- renaming is a breaking change for existing consumers.

---

## Phase D: Developer Experience

> **Goal:** Make Forge easy to adopt. Template, tooling, testing.
> **Estimated total:** 3-4 weeks
> **Can run in parallel with Phase C.**

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| D1 | EditorConfig + Roslyn analyzers (Issue #1) | M | 4 | Lo | None | **Itenium.Forge.CodeStyle** (attempted, parked) |
| D2 | `ForgeWebApplicationFactory` (test helper) | M | 5 | Lo | Stable B1, Security | **New: Itenium.Forge.Testing** |
| D3 | Authenticated HttpClient test helper | S | 4 | Lo | D2, Security | Testing |
| D4 | Test data builders (Forge types) | S | 2 | Lo | D2 | Testing |
| D5 | MassTransit test harness auto-config | S | 3 | Lo | D2, B9 | Testing |
| D6 | Local dev docker-compose (full stack) | L | 4 | Med | C1, C4 (Grafana), B9 (RabbitMQ) | Ships with template repo |
| D7 | Template repository (`dotnet new forge-api`) | L | 5 | Med | All Phase A+B stable, D6 | **New: Itenium.Forge.Templates** |
| D8 | Feature flags (`Microsoft.FeatureManagement`) | S | 3 | Lo | Settings | Settings (extend) or new package |
| D9 | Localization (`AddForgeLocalization()`) | M | 3 | Med | Controllers | **New: Itenium.Forge.Localization** |
| D10 | Reusable CI/CD workflow (GitHub Actions) | M | 4 | Lo | D7 | Ships with template repo |
| D11 | Base Dockerfile | S | 3 | Lo | D7 | Ships with template repo |

**Phase D critical path:** D2 → D3/D4/D5 (testing), then D7 depends on everything being stable.

---

## Phase E: Runtime Layer

> **Goal:** Bridge between chassis and ForgeBuilder. Auto-CRUD, metadata, field permissions.
> **Estimated total:** 6-10 weeks
> **Hard dependency:** Phase B complete (especially B1, B2, B7)

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| E1 | Metadata API (compile-time + runtime, single source of truth) | XL | 5 | Hi | B1, B2, A6 | **New: Itenium.Forge.Metadata** |
| E2 | Auto-CRUD / generic controller | XL | 5 | Hi | E1, B1, B2, B3, A6 | **New: Itenium.Forge.AutoCrud** |
| E3 | Field-level permissions (visible/hidden/readonly per role) | L | 4 | Med | E1, Security | Metadata or Security (extend) |
| E4 | Command sourcing (CQRS command log) | L | 3 | Hi | B1 | **New: Itenium.Forge.CommandSourcing** |
| E5 | TypeScript client generation (orval / openapi-typescript) | L | 4 | Med | Swagger, E1 | CI tooling / npm package |
| E6 | SignalR real-time entity broadcasts | M | 4 | Med | E2 (auto-CRUD fires events) | **New: Itenium.Forge.SignalR** |
| E7 | Optimistic concurrency / conflict resolution | M | 4 | Med | B1, E2 | Data (extend) or AutoCrud |
| E8 | Template project structure (Api + Contracts + Services + Tests) | M | 3 | Lo | D7 | Templates (extend) |

**Phase E critical path:** E1 → E2 → E3 (metadata drives everything)

### Phase E open design questions

- E1: How do compile-time C# ValueTypes merge with runtime ForgeBuilder overrides? Needs architecture spike.
- E2: Generic controller vs scaffolded vs hybrid? Generic for CRUD, specific when overridden.
- E5: Code-first (C# → OpenAPI → TS) confirmed. Tool choice needs investigation.

---

## Phase F: ForgeBuilder Platform

> **Goal:** The product. Admin wizard, UI builder, design system, ejectability.
> **Estimated total:** Multi-quarter
> **Hard dependency:** Phase E (especially E1, E2)

| # | Item | Effort | Value | Risk | Dependencies | Package |
|---|------|--------|-------|------|--------------|---------|
| F1 | ForgePencil design system (npm, shadcn/ui + tokens) | L | 5 | Med | None (can start early) | **npm: @itenium/forge-pencil** |
| F2 | Storybook for ForgePencil | M | 4 | Lo | F1 | Part of ForgePencil |
| F3 | Admin wizard (branding, languages, auth config) | L | 4 | Med | F1, E1 | **ForgeBuilder app** |
| F4 | UI builder (grid-based form/page designer) | XL | 5 | Hi | F1, F3, E1, E2 | ForgeBuilder app |
| F5 | Block system (table, detail, form, dashboard) | XL | 5 | Hi | F4, E2, E6 | ForgeBuilder app |
| F6 | Config file structure (JSON/YAML, Git output) | L | 5 | Hi | F3, F4 | ForgeBuilder app |
| F7 | Config interpreter (frontend runtime reads config) | XL | 5 | Hi | F5, F6 | **npm: @itenium/forge-runtime** |
| F8 | Eject to plain React codebase | L | 4 | Hi | F7 (design for from day one) | ForgeBuilder app |
| F9 | Dark mode (token validation) | S | 3 | Lo | F1 | ForgePencil |
| F10 | Layout system (table/cards/calendar/kanban/map per entity) | L | 4 | Med | F5 | ForgeBuilder app |

**Phase F critical path:** F1 → F4 → F5 → F6 → F7

### ForgeBuilder tracer bullets (milestones)

1. **Tracer 1:** ForgeBuilder → one text field "Name" → deploy → prints name. Proves full loop. (F3 + F6 + F7 minimal)
2. **Tracer 2:** Add second microservice frontend, lazy loaded. Proves multi-app composition. (F7 + shell routing)
3. **Tracer 3:** Dead letter monitor with full CRUD. Proves real block system. (F5 + E2 + B9)

---

## Dogfooding Microservices

> **Goal:** Real apps built on Forge to validate the chassis. Built as needed, not all at once.
> **These are products, not packages.**

| # | Service | Depends On | Effort | When |
|---|---------|-----------|--------|------|
| Dog1 | Dead Letter Monitor | B9, E2, F5 | L | After Phase E (Tracer 3) |
| Dog2 | Identity Admin | Security, E2, F5 | L | After Phase E |
| Dog3 | Audit Log Viewer | B4, E2, F5 | M | After B4 + Phase E |
| Dog4 | Notification Service | B9, E6, F5 | L | After Phase E |
| Dog5 | File Storage Service | B1, E2 | M | When file upload needed |
| Dog6 | Feature Flag Manager | D8, E2, F5 | M | After D8 + Phase E |

---

## UX Features (ForgeBuilder blocks/capabilities)

> **These are features OF ForgeBuilder, not standalone packages. Built incrementally within F4/F5.**

| Priority | Feature | Effort | Value | Phase |
|----------|---------|--------|-------|-------|
| P1 | Keyboard-first navigation | M | 4 | F5 |
| P1 | View/edit mode configuration | S | 4 | F5 |
| P1 | Form crash recovery (localStorage) | S | 4 | F7 |
| P1 | Undo/redo on forms | M | 4 | F7 |
| P2 | Smart table state persistence | M | 4 | F5 |
| P2 | Bulk operations (multi-select) | M | 3 | F5 |
| P2 | Global search / command palette | M | 4 | F7 |
| P2 | Contextual help / field docs | S | 3 | F5 |
| P2 | Print / PDF / Excel export | M | 3 | F5 + Dog5/7/8 |
| P2 | Concurrent edit conflict resolution | L | 4 | E7 + F5 |
| P2 | Multi-tab awareness (BroadcastChannel) | S | 3 | F7 |
| P3 | Audit trail in UI | M | 3 | B4 + F5 |
| P3 | Favorites / pinned records | S | 2 | F5 |
| P3 | Notifications / activity feed | M | 3 | Dog4 + F5 |
| P3 | Dashboard builder (widgets) | L | 3 | F5 |
| P3 | Draggable/resizable modals | M | 3 | F7 |
| P3 | Mobile responsiveness | M | 3 | F1 (tokens) + F5 |
| P3 | Drag-and-drop reordering | M | 2 | F5 |
| P3 | File preview (image/PDF lightbox) | S | 2 | Dog5 + F5 |

---

## Backlog / Deferred

| Item | Reason | Revisit When |
|------|--------|--------------|
| gRPC bootstrap | YAGNI now | When a team needs it |
| Content negotiation | JSON-only sufficient | Unlikely |
| MediatR integration | Stay out of MediatR's lane | Never |
| Multi-tenancy | Not a cloud product | Design for it (B13 carries `TenantId`), build when needed |
| Forge CLI (`dotnet tool`) | Nice but not critical | After template repo |
| Contract testing (Pact) | Needs foundation first | After E5 (TS generation) |
| Cache invalidation via events | Complex, easy to get wrong | After B11 + B9 stable |
| Response/output caching | Dangerous (stale data) | Needs careful design |
| Workflow engine | Large scope | When a real app needs it |
| Middleware ordering detection | Needs analysis | After all Use* methods exist |
| ForgeBuilder concurrent editing | Git handles it initially | When builder has multiple users |
| Reactive expression bindings (`{{ }}`) | Low-code abstraction, complex | Phase F mature |
| Named queries as first-class objects | Retool pattern, needs design | Phase F mature |
| Entity DSL (JDL-like) | Source generators, complex | After E1 |
| Auto-controller convention (`[ForgeAutoApi]`) | Needs investigation vs E2 | E2 design phase |
| Event wiring model | Declarative actions in config | Phase F mature |
| Scaffold CLI command | Nice DX, not critical path | After D7 |

---

## Recommended Execution Order

```
                    +-----------+
                    | Phase A   |  2-3 weeks
                    | Foundation|
                    +-----+-----+
                          |
                    +-----v-----+
                    | Phase B   |  4-6 weeks
                    | Data+Msg  |
                    +-----+-----+
                          |
              +-----------+-----------+
              |                       |
        +-----v-----+          +-----v-----+
        | Phase C   |          | Phase D   |  3-4 weeks each
        | Obs+Ops   |          | Dev Exp   |  (parallel)
        +-----+-----+          +-----+-----+
              |                       |
              +-----------+-----------+
                          |
                    +-----v-----+
                    | Phase E   |  6-10 weeks
                    | Runtime   |
                    +-----+-----+
                          |
                    +-----v-----+
                    | Phase F   |  multi-quarter
                    | Builder   |
                    +-----------+

    F1 (ForgePencil) can start any time -- no backend deps
```

### Quick wins to do first (within Phase A)

1. **A1** Correlation ID -- half-done, critical for everything
2. **A2** Config validation -- trivial, prevents production misconfig
3. **A3** Security headers -- trivial, security baseline
4. **A6** `IForgePagedResult<T>` -- get the contract shape right early (risk item)
5. **A8** Machine-name appsettings -- trivial, immediate local dev benefit

### Strategic items to design early (even if built later)

1. **E1** Metadata API shape -- drives ForgeBuilder, AutoCrud, TS generation. Spike early.
2. **B13** `IForgeContext` -- carries tenant, correlation, user. Design once.
3. **F6** Config file structure -- affects all of ForgeBuilder. Research early.
4. **A6** `IForgePagedResult<T>` -- API contract, hard to change once published.

---

## New Package Summary

| Package | Phase | Contents |
|---------|-------|----------|
| Itenium.Forge.Data | B | DbContext registration, interceptors (audit, soft delete, row-change), migration helpers, paging extensions |
| Itenium.Forge.HttpClient | B | Typed HttpClient factory, correlation ID forwarding, resilience pipelines |
| Itenium.Forge.MassTransit | B | MassTransit + RabbitMQ wiring, outbox, correlation propagation |
| Itenium.Forge.Caching | B | Redis distributed cache, in-memory fallback |
| Itenium.Forge.BackgroundServices | B | Background service base, Quartz scheduling |
| Itenium.Forge.Vault | C | HashiCorp Vault secrets → IConfiguration |
| Itenium.Forge.Testing | D | ForgeWebApplicationFactory, auth helpers, test data builders, MassTransit harness |
| Itenium.Forge.Templates | D | `dotnet new forge-api` template pack |
| Itenium.Forge.Metadata | E | Metadata API, field types, compile-time + runtime merge |
| Itenium.Forge.AutoCrud | E | Generic CRUD controller, entity-first development |
| Itenium.Forge.SignalR | E | Real-time entity change broadcasts |
| Itenium.Forge.CommandSourcing | E | CQRS command log (advanced, opt-in) |

### Existing packages to extend

| Package | New capabilities |
|---------|-----------------|
| Core | `IForgePagedResult<T>`, `IForgeContext`, `IAuditable`, `ISoftDeletable` interfaces |
| Settings | Config validation, machine-name overrides, feature flags |
| Logging | Correlation ID middleware, field masking, OTEL tracing+metrics, runtime log level |
| Controllers | API versioning, response compression, request validation |
| Security | Security headers, rate limiting (or new package) |
| HealthChecks | Auto-registration, build info enrichment |
| Swagger | API version support |

---

## Open Decisions (need resolution before or during implementation)

| # | Decision | Impact | Suggested Resolution |
|---|----------|--------|---------------------|
| 1 | `IForgePagedResult<T>` shape (offset vs cursor, metadata fields) | A6, all consumers | Design spike in Phase A |
| 2 | Rate limiting: own package or fold into Security? | A5 | New package (independent concern) |
| 3 | API versioning: own package or fold into Controllers? | A4 | Extend Controllers + Swagger |
| 4 | Rename Logging → Observability? | C1 | No -- breaking change, keep Logging |
| 5 | `IForgeContext` location (Core vs new package) | B13 | Core (no heavy deps) |
| 6 | Validation library (FluentValidation vs DataAnnotations) | B15 | FluentValidation (Apache 2.0, richer) |
| 7 | Full row-change audit: same DB vs separate sink? | B4 | Separate sink recommended, configurable |
| 8 | Metadata API: compile-time + runtime merge strategy | E1 | Architecture spike before Phase E |
| 9 | Auto-CRUD: generic controller vs scaffolded vs hybrid | E2 | Generic + override pattern |
| 10 | TS client generation tool (orval vs NSwag vs Kiota) | E5 | Investigate during Phase D |
| 11 | ForgeBuilder config format (JSON vs YAML) | F6 | Research during Phase E |
| 12 | Config interpreter: thin reader or framework? | F7 | Thin reader (ejectability) |

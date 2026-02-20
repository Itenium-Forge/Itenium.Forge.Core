---
stepsCompleted: [1, 2, 3, 4, 5]
inputDocuments: [README.md, GitHub Issue #1, GitHub Issue #2]
session_topic: 'Microservice chassis gaps in Itenium.Forge + ForgeBuilder vision'
session_goals: 'Identify missing features, prioritize next packages/capabilities for a complete microservice chassis, define ForgeBuilder application platform vision'
selected_approach: 'progressive-flow'
techniques_used: ['morphological-analysis', 'six-thinking-hats', 'first-principles', 'cross-pollination', 'question-storming']
ideas_generated: 100+
context_file: ''
---

# Brainstorming Session Results

**Facilitator:** Wouter
**Date:** 2026-02-19 / 2026-02-20 / 2026-02-20 (session 3)

## Session Overview

**Topic:** What is missing in Itenium.Forge to be a complete microservice chassis, and the ForgeBuilder application platform vision.
**Goals:** Identify gaps, prioritize next packages/capabilities, surface non-obvious needs, define ForgeBuilder as the end-goal product.

### Context Guidance

**Current Forge coverage (8 packages):**
- Core (ForgeSettings POCO), Settings (appsettings loading), Logging (Serilog + Loki), Controllers (MVC, CORS, JSON), Swagger (Swashbuckle), Security (ICurrentUser + Keycloak + OpenIddict), HealthChecks (K8s liveness/readiness), ProblemDetails (RFC 7807)

**README TODOs already identified:**
- Template Repository
- Roslynator / Code-Style enforcement
- Secrets in Vault/Consul
- x-correlation-id header forwarding
- Circuit Breaker

**GitHub Issue #1:** Code-Style across projects (editorconfig, Roslyn analyzers, enforce on CI)
**GitHub Issue #2:** DbContext base, Swagger enhancements, JWT improvements, audit logs in EF Core

## Key Architectural Decisions

- **ForgeBuilder is the vision** -- the chassis is infrastructure beneath the product
- **Interfaces over base classes** -- `IAuditable { AuditInfo Audit { get; set; } }` not `AuditableEntity` base
- **Composition over inheritance** -- minimize lock-in, no forced base classes
- **Extension methods for bootstrap** -- `AddForge*()` / `UseForge*()` pattern
- **Configuration over code, code as escape hatch** -- ForgeBuilder configures the 80%, code the 20%
- **Design for ejectability from day one** -- ForgeBuilder can "eject" to a regular React codebase
- **ForgeBuilder is standalone** -- doesn't ship with the deployed app, outputs config files to Git (GitOps)
- **All dependencies MIT/Apache 2.0 licensed**

---

## Technique Execution Results

### Phase 1: Morphological Analysis -- Chassis Dimensions

#### Dimension 1: Communication (Sync HTTP)

**[Comm #1]: HttpClientFactory with Defaults** -- YES
`AddForgeHttpClient<TClient>("service-name")` -- registers typed HttpClient with: correlation-id forwarding, Forge user-agent, base address from config, JSON defaults. Zero lock-in, standard HttpClient.

**[Comm #2]: Correlation ID Middleware** -- YES (critical)
`UseForgeCorrelationId()` -- reads/generates x-correlation-id, sets on Activity.Current + Serilog context, auto-forwards on outgoing HttpClient calls via delegating handler. Half-done already in Serilog enricher.

**[Comm #3]: Standard Response Envelope / Pagination** -- YES
`IForgePagedResult<T>` as interface in Core. Services implement it, frontends code against the contract. Grouped in a ValueObject. Critical for ForgeBuilder table blocks.

**[Comm #4]: gRPC Bootstrap** -- BACKLOG

**[Comm #5]: Content Negotiation** -- NO (JSON-only, YAGNI. Can be extended later if needed)

#### Dimension 2: Communication (Async / Messaging)

**Decision:** Forge doesn't abstract messaging, it integrates with it. MassTransit + RabbitMQ (both MIT/Apache 2.0).

**[Async #1]: MassTransit Integration** -- YES
`AddForgeMassTransit()` wires up MassTransit + RabbitMQ with Forge conventions: correlation-id propagation, Serilog enrichment, health check registration. Extension methods only, no abstraction layer.

**[Async #2]: Outbox Pattern Support** -- YES
Lean on MassTransit's built-in `AddEntityFrameworkOutbox()`. Forge configures it as part of `AddForgeMassTransit()`.

**[Async #3]: MediatR / In-Process Events** -- NO (stay out of MediatR's lane)

**[Async #4]: Consumer/Handler Registration** -- NO (let MassTransit handle its own)

**Open question:** Error queue monitoring after retry exhaustion. Dashboard/microservice with UI for manual retry/discard by dev/admin/poweruser. Next business day handling. This becomes a dogfood microservice.

#### Dimension 3: Data

**[Data #1]: EF Interceptor for Audit Logging** -- YES
`SaveChangesInterceptor` populates `CreatedAt/By`, `ModifiedAt/By` on `IAuditable` entities. Uses `ICurrentUser`. `AuditInfo` is a value object / owned type.

**[Data #2]: Soft Delete Interceptor** -- YES
`ISoftDeletable` interface. Interceptor converts `Remove()` to flag. Global query filter auto-applied.

**[Data #3]: Auto-Migration on Startup** -- YES
`UseForgeAutoMigration<TContext>()` for dev. K8s Job for production. Deployment strategy for running migrations needs analysis.

**[Data #4]: DbContext Registration Helper** -- YES
`AddForgeDbContext<TContext>(options => ...)` registers context + applies interceptors + configures connection string. No base class (hopefully). If base class needed, revisit.

**[Data #5]: Connection String Management** -- YES
Convention: `ConnectionStrings:Default` or `ConnectionStrings:{ServiceName}`. Also needs Vault integration for deployment.

**[Data #6]: Repository Pattern** -- UNDECIDED
`DbSet<T>` is already a repository. BUT paged/filtered/sorted queries have real logic. Possible middle ground: extension methods on `IQueryable<T>` (`query.ApplyForgePagedRequest().ToForgePagedResult()`). Not a repository class. To be evaluated.

**[Data #7]: Outbox Table + Processor** -- YES
MassTransit's EF outbox. Configured via `AddForgeMassTransit().WithOutbox()`.

**[Data #8]: Full Row Change Audit Logging** -- YES
Beyond IAuditable: before/after snapshots on every DB change. EF interceptor serializes changed properties to audit table. Opt-in per entity via interface. Configurable retention.

**[Data #9]: Command Sourcing** -- YES (large effort)
Save inbound command/request objects for replay, debugging, compliance. CQRS-style command log. Separate package, advanced pattern.

#### Dimension 4: Resilience

**[Res #1]: Resilience Pipeline for HttpClient** -- YES
`Microsoft.Extensions.Http.Resilience` (MIT, official MS library on Polly v8). Default: retry 3x exponential + circuit breaker + timeout. Smart logging + health check integration. Bundled into `AddForgeHttpClient<T>()`.

**[Res #2]: Resilience Defaults from Config** -- YES
`Forge:Resilience` section in appsettings. Per-client overrides via `Forge:Resilience:Clients:{name}`.

**[Res #3]: Resilience for MassTransit** -- YES (complex)
MassTransit's own retry/circuit breaker configured via `AddForgeMassTransit()`. Open question: what happens after exhaustion? Error queue monitoring, dashboard, manual action. Needs separate microservice.

**[Res #4]: Rate Limiting** -- YES
`AddForgeRateLimiting()` with `Microsoft.AspNetCore.RateLimiting`. Stricter limits on auth endpoints. Log at Warning when rate limit hit + Grafana dashboard. Also log when no policy covers an endpoint (config gap detection).

**[Res #5]: Bulkhead** -- ALREADY COVERED by #1's resilience pipeline

#### Dimension 5: Observability

**[Obs #1]: OpenTelemetry Tracing** -- YES
`AddForgeTracing()` -- auto-instrumentation for ASP.NET Core, HttpClient, EF Core, MassTransit. Exports to OTLP (Jaeger/Tempo). Code already commented-out in LoggingExtensions.

**[Obs #2]: OpenTelemetry Metrics** -- YES
`AddForgeMetrics()` -- Prometheus-compatible. Auto-instruments request duration/count, GC, EF queries, MassTransit throughput.

**[Obs #3]: Correlation ID** -- already tracked in Comm #2

**[Obs #4]: Health Check Enrichment** -- YES
Auto-register health checks when other Forge packages are added (DB, Redis, RabbitMQ). Include build timestamp, git commit hash, uptime. Auto-discovery: `AddForgeDbContext()` → DB health check appears.

**[Obs #5]: Grafana Dashboard Templates** -- YES
Ship JSON dashboards for Grafana. Pre-configured for Forge label conventions. Deployment should spin up Grafana with dashboards already working.

**[Obs #6]: Alerting Rules** -- YES
Prometheus alert rules: high error rate, latency spikes, health check failures.

**Open question:** Rename `Itenium.Forge.Logging` to `Itenium.Forge.Observability`? Or keep separate? Decision for architecture phase.

#### Dimension 6: Security

**[Sec #1]: Secrets Management (Vault)** -- YES
`AddForgeVault()` with VaultSharp (MIT). Loads secrets into IConfiguration. Critical for production.

**[Sec #2]: Rate Limiting on Auth Endpoints** -- YES (part of Res #4, stricter defaults)

**[Sec #3]: API Key Authentication** -- DEPENDS
For daemon/background processes without user context (outbox processor, scheduled jobs). JWT forwarding covers user-initiated service-to-service calls. API key for non-user scenarios.

**[Sec #4]: HTTPS Enforcement** -- KILL (reverse proxy handles TLS)

**[Sec #5]: Security Headers Middleware** -- YES
Wrap into `UseForgeSecurity()` as one big call. Individual methods as fallback.

**[Sec #6]: Claims Enrichment Pipeline** -- NOTED (might need, ASP.NET Core's IClaimsTransformation may suffice)

#### Dimension 7: Configuration

**[Cfg #1]: Startup Validation** -- YES
`ValidateOnStart()` with DataAnnotations. Fail fast on bad config.

**[Cfg #2]: Vault/Consul Integration** -- already tracked in Sec #1

**[Cfg #3]: Feature Flags** -- YES (integrate)
`Microsoft.FeatureManagement` (MIT). `AddForgeFeatureFlags()`.

**[Cfg #4]: Environment-Specific Overrides** -- YES
`appsettings.{MachineName}.json` for local dev.

**[Cfg #5]: Config Debug Endpoint** -- YES
`/config` dev-only endpoint, secrets masked. Schema validation already solved.

**API Versioning** -- YES
`AddForgeApiVersioning()` with `Asp.Versioning.Mvc` (MIT). URL segment (`/api/v1/`). Deprecation support. Bolted into Forge. Swagger/OpenAPI replacement shows all versions.

#### Dimension 8: API Design

**[API #1]: API Versioning** -- tracked above

**[API #2]: Standard Error Codes** -- KILL (ProblemDetails is the standard, no error code taxonomy)

**[API #3]: Request/Response Logging Filtering** -- YES
Mask sensitive fields in request logging. Config-driven: `Forge:Logging:MaskFields: ["password", "ssn"]`. GDPR compliance.

**[API #4]: Response Compression** -- YES (fold into Controllers, trivial)

**Request Validation** -- YES
Validation layer needed. Library choice undecided (FluentValidation Apache 2.0 is an option, but not committed). Integration with ProblemDetails for field-level errors. Frontend sync via OpenAPI schema annotations.

**Localization** -- YES
`AddForgeLocalization()`. Request culture from Accept-Language header. Validation errors and ProblemDetails localized. Resource format (resx vs JSON) TBD.

#### Dimension 9: Developer Experience

**[DX #1]: Template Repository** -- YES (critical for adoption)
`dotnet new forge-api`. Scaffolds full microservice. Published as NuGet template pack. Depends on all other packages being stable.

**[DX #2]: EditorConfig + Roslyn Analyzers (Issue #1)** -- YES
NuGet package (`Itenium.Forge.CodeStyle`) auto-imports props. Errors in CI/Release, warnings in Debug.

**[DX #3]: Local Dev Docker Compose** -- YES
`docker-compose.dev.yml` with PostgreSQL, RabbitMQ, Keycloak, Grafana+Loki+Tempo+Prometheus (pre-configured), Vault. Profiles for minimal setup.

**[DX #4]: Forge CLI / dotnet tool** -- BACKLOG

**[DX #5]: Migration Guide** -- KILL (just semver + CHANGELOG)

#### Dimension 10: Deployment

**[Deploy #1]: Base Dockerfile** -- YES (ships with template repo)

**[Deploy #2]: K8s Manifests / Helm Chart** -- NEEDS ANALYSIS

**[Deploy #3]: DB Migration as Job** -- YES
K8s Job for prod, `UseForgeAutoMigration()` for dev.

**[Deploy #4]: Docker Compose Full Stack** -- tracked in DX #3

**[Deploy #5]: Observability Stack Deployment** -- YES (Grafana + dashboards pre-loaded)

**[Deploy #6]: Reusable CI/CD Workflow** -- YES
GitHub Actions reusable workflow. Same pipeline for every Forge microservice.

#### Dimension 11: Cross-Cutting

**[Cross #1]: Correlation ID** -- tracked in Comm #2

**[Cross #2]: Multi-Tenancy** -- BACKLOG (add when needed)

**[Cross #3]: IForgeContext (AsyncLocal)** -- YES
Carries correlation ID, current user, tenant, trace ID. Flows through async calls and message handlers. Solves "no HttpContext in background job" problem.

**[Cross #4]: Middleware Ordering Detection** -- NEEDS ANALYSIS
Detect misconfigured middleware order at startup. Valuable but must not be annoying.

#### Dimension 12: Testing

**[Test #1]: ForgeWebApplicationFactory** -- YES
`Itenium.Forge.Testing` package. Pre-configured factory: swaps external deps, test ICurrentUser, disables background services.

**[Test #2]: Authenticated HttpClient Helper** -- YES
`factory.CreateClientAs(userId, roles)`. Returns HttpClient with valid test JWT.

**[Test #3]: Test Data Builders** -- YES (Forge types only, app types are team's job)

**[Test #4]: Contract Testing** -- BACKLOG (high value with API versioning, but needs foundation first)

**[Test #5]: MassTransit Test Harness** -- YES
Auto-configure InMemoryTestHarness when MassTransit detected.

#### Dimension 13: Background Processing

**[Bg #1+#2]: Background Services + Quartz** -- YES
`AddForgeBackgroundService<T>()` with logging, health check, correlation ID. Quartz.NET (Apache 2.0) for cron scheduling. One package.

**[Bg #3]: Outbox Processor** -- tracked in Data #7

**[Bg #4]: Dead Letter Monitor** -- YES (dogfood microservice with frontend)

#### Dimension 14: Caching

**[Cache #1]: Distributed Cache Setup** -- YES
`AddForgeCaching()` with Redis (StackExchange.Redis, MIT). Fallback to in-memory. Health check auto-registered.

**[Cache #2]: Response Caching** -- NEEDS DISCUSSION (dangerous, stale data)

**[Cache #3]: Output Caching** -- NEEDS DISCUSSION (same concern)

**[Cache #4]: Cache Invalidation via Events** -- BACKLOG

---

### Phase 1 (continued): ForgeBuilder Platform

#### Vision

ForgeBuilder is the end-goal product. The chassis is infrastructure beneath it. ForgeBuilder is a standalone design tool that outputs config files to Git. The deployed app has no ForgeBuilder dependency.

**Flow:**
```
ForgeBuilder (standalone) → config files (Git) → CI/CD → deployed frontend app
```

**Multi-app platform:**
- One ForgeBuilder instance manages multiple apps
- Each app can compose multiple microservice frontends (lazy loaded)
- Same microservice can appear in multiple apps with different views/permissions
- Single-microservice apps also supported

#### ForgeBuilder Concepts

**[FB #1]: Entity-First Development / Auto-CRUD** -- NEEDS ARCHITECTURE DECISION
Generic controller vs scaffolded vs hybrid. Generic handles basic CRUD, specific controller overrides when needed. Tension between magic and explicitness. Needs proper design session.

**[FB #2]: Metadata API** -- YES (critical path)
Custom format (not just OpenAPI -- too limited for validation, permissions, display hints). Layered:
- Compile-time: C# types (ValueTypes carry semantic meaning: Money, Email, Phone, DateRange)
- Runtime: Admin overrides (labels, translations, display preferences, field ordering, visibility per role)
Frontend reads runtime metadata API to render.

**[FB #3]: Field-Level Permissions** -- YES
Visible/hidden/readonly per role, per entity. Deploy-time config (avoid careless mistakes). Chassis provides enforcement middleware, ForgeBuilder provides config UI.

**[FB #4]: Admin Wizard** -- YES
Configures: branding (logo, colors, name), languages, auth, entity field metadata, navigation (with layout templates: header+content, header+leftnav+content, etc.), pages.

**[FB #5]: UI Builder** -- YES
Structured form/wizard (grid-based, breakpoints). Not drag-and-drop. Blocks compose (table click → detail, etc.). React specifically.

**[FB #6]: Workflow Engine** -- BACKLOG

**[FB #7]: Eject Option** -- YES (design for it from day one)
"Eject" generates a regular React codebase. One-way door: no more ForgeBuilder upgrades. Needs to be designed in from the start even if not built immediately.

**[FB #8]: Config File Structure** -- DEFERRED (needs research)
JSON or YAML, file organization, build-time vs runtime loading. To be analyzed.

#### ForgeBuilder Tracer Bullets

**Tracer 1:** ForgeBuilder → configure app with one text field "Name" → deploy → new app prints the name. Proves the full loop.

**Tracer 2:** Add second microservice frontend to the app → lazy loaded in same shell. Proves multi-microservice composition.

**Tracer 3:** Dead letter monitor with full CRUD (table + detail + form + actions). Proves real block system.

#### Design System

**ForgePencil (npm package):**
- Design tokens (spacing, colors, typography, breakpoints)
- shadcn/ui components customized with Forge tokens (already set up)
- Composite components (ForgeBuilder blocks)
- Tailwind CSS as styling approach
- Theming via CSS custom properties (ForgeBuilder branding config swaps these)
- Tailwind config preset enforcing token scale

**Storybook:**
- Every ForgePencil component documented with examples
- All field types, all states (error, disabled, readonly, loading)
- Composite blocks with mock data
- Documentation for teams building custom React components

**Pencil Integration:**
- AI-friendly design tool (existing product, research when needed)

#### Field Type Set (full from day one)

| Field Type | Input | Notes |
|---|---|---|
| Text | Single line | maxlength, placeholder |
| TextArea | Multi-line | rows config |
| Number | Numeric | min/max, step |
| Money | Numeric + currency | locale-aware formatting |
| Date | Date picker | min/max date |
| DateTime | Date + time picker | timezone handling |
| Boolean | Toggle/checkbox | |
| Dropdown | Select | static list or from endpoint |
| Multi-Select | Multi-choice | tags-style |
| Email | Text + validation | |
| Phone | Text + formatting | |
| URL | Text + validation | |
| File | Upload zone | ties to file storage service |
| Readonly | Display only | any type as text |
| Hidden | Not rendered | passed in form data |

---

### Phase 1 (continued): UX Killer Features

**[UX #1]: SignalR Real-Time Updates** -- YES
Backend auto-broadcasts entity changes. Tables auto-update. Detail views show "modified by X." Configurable per entity. `AddForgeSignalR()` fires notifications from auto-CRUD for free.

**[UX #2]: View/Edit Mode Configuration** -- YES
Per-app or per-entity: `defaultMode: "view" | "edit" | "inline"`. Back-office = edit mode (speed), customer portal = view mode (safety).

**[UX #3]: Concurrent Edit Conflict Resolution** -- YES
Optimistic concurrency via ETag/row version. Save conflict shows diff with merge options. SignalR warns in real-time while editing: "User Y is also editing this record."

**[UX #4]: Smart Table State Persistence** -- YES
Per-user: column visibility, order, sort, filters, page size. Saved to backend. Export visible columns to Excel. Save/share filter presets.

**[UX #5]: Form Crash Recovery** -- YES
Auto-save to localStorage (debounced). On reload: "You have unsaved changes from 14:32. Restore?" Covers: crash, accidental close, navigation, session timeout.

**[UX #6]: Draggable/Resizable/Minimizable Modals** -- YES
Drag, resize, minimize to taskbar strip. Multiple modals open simultaneously. Compare records side by side. Desktop-like experience.

**[UX #7]: Undo/Redo** -- YES
Ctrl+Z/Ctrl+Y on any form change. Undo dropdown change, undo row delete (soft delete), undo drag reorder. Toast: "Record deleted. [Undo]" with countdown.

**[UX #8]: Global Search / Command Palette** -- YES
Ctrl+K. Search across all entities/microservices. Navigate to any page. Trigger actions. Spotlight/VS Code style.

**[UX #9]: Audit Trail in UI** -- YES
History tab on any detail view. Who changed what, when, field-level diff. Point-in-time snapshots. Powered by row-change audit from chassis.

**[UX #10]: Keyboard-First Navigation** -- YES
Full keyboard nav. Tab through rows, Enter to open, Escape to close. Configurable hotkeys. ARIA/accessibility as side effect.

**[UX #11]: Bulk Operations** -- YES
Multi-select rows. Bulk delete, status change, export, reassign. Progress bar. Configurable per entity in ForgeBuilder.

**[UX #12]: Contextual Help / Field Documentation** -- YES
Help icon per field: description, expected format, example. Page-level help panel. Configured in ForgeBuilder per field per language.

**[UX #13]: Favorites / Pinned Records** -- YES
Star any record. Favorites in sidebar or command palette. Per-user, synced to backend.

**[UX #14]: Notifications / Activity Feed** -- YES
Bell icon with SignalR real-time notifications. Subscribe to changes on specific records or filters. In-app + email + push via notification microservice.

**[UX #15]: Dashboard Builder** -- YES
Special page type: KPI tiles, charts, tables, activity feeds. Each widget from different microservice. Auto-refresh configurable.

**[UX #16]: Print / PDF / Excel Export** -- YES
Tables: export visible columns (respecting user config) to Excel (ClosedXML, MIT) or PDF (via PDF microservice). Detail views: print-friendly / PDF. What you see is what you export.

**[UX #17]: Dark Mode** -- YES (high priority)
Toggle in header. Per-user preference. ForgePencil tokens light/dark variants. Respects OS `prefers-color-scheme`. Validates entire token architecture.

**[UX #18]: Multi-Tab Awareness** -- YES
BroadcastChannel API for same-user multi-tab. Combined with SignalR for multi-user. Combined with conflict resolution on save. Full coverage.

**[Misc #1]: Mobile Responsiveness** -- YES
Grid breakpoints. Table → card list on mobile. Forms single column. Hamburger nav. Configure which columns show on mobile.

**[Misc #2]: Drag-and-Drop Reordering** -- YES
Tables/card grids with manual sort order. Block config: `orderable: true`.

**[Misc #3]: File Preview** -- YES
Inline preview: image thumbnails, PDF viewer, lightbox. Combined with file storage microservice.

---

### Dogfooding Microservices (built when needed in real apps)

| # | Service | Frontend | Description |
|---|---------|----------|-------------|
| 1 | Dead Letter Monitor | Admin UI | Monitor MassTransit error queues, manual retry/discard |
| 2 | Identity Admin | Admin panel | Manage users, roles, capabilities, clients, audit log |
| 3 | Feature Flag Manager | Admin panel | Toggle flags across services |
| 4 | Audit Log Viewer | Compliance UI | Who changed what, when, field diffs, point-in-time |
| 5 | Notification Service | Template mgmt + history | Centralized routing: email, SMS, push, in-app |
| 6 | File Storage Service | File browser + upload | S3/Azure Blob/local abstraction |
| 7 | PDF Creator | -- | Receive requests via MassTransit, generate PDFs |
| 8 | Excel Creator | -- | Receive requests via MassTransit, generate Excel |
| 9 | Configuration Manager | Admin panel | Central config/feature flag management |
| 10 | AppBuilder (ForgeBuilder) | The builder itself | Ultimate dogfood |

---

### Phase 2: Six Thinking Hats Summary

#### White Hat (Facts)
- ~10 capabilities small effort (days), ~12 medium (weeks), ~6 large (sprints)
- All dependencies MIT/Apache 2.0
- Half the observability story already exists (Serilog)
- OpenTelemetry code already commented-out in LoggingExtensions

#### Black Hat (Risks)
- `IForgePagedResult<T>` bleeds into API contracts -- get the shape right early
- MassTransit wrapping too tightly makes upgrades hard -- thin wrapper only
- Full row-change audit generates massive data -- opt-in, configurable retention
- Command sourcing / CQRS is architectural commitment -- separate package, advanced
- Docker compose with 8+ containers needs profiles for minimal setup
- Dogfood microservices are real products that consume bandwidth
- Template repo depends on stable packages -- ship last

#### Yellow Hat (Value)
**Biggest bang for buck (high value, low effort):**
1. Correlation ID middleware
2. Startup config validation
3. Security headers
4. API versioning
5. Rate limiting

**Highest strategic value:**
1. Itenium.Forge.Data (DbContext + interceptors)
2. OpenTelemetry
3. MassTransit integration
4. HttpClientFactory + resilience
5. Template repo

#### Red Hat (Gut) -- led to ForgeBuilder vision
AppBuilder/ForgeBuilder as the ultimate product. Configure backend from template, wizard to configure the app (branding, languages), build UI from lego blocks, plugin custom React components.

#### Green Hat (Creative)
**Architecture layers:**
1. Forge Chassis (what exists + gaps)
2. Forge Runtime (auto-CRUD, metadata API, field permissions, plugin system)
3. ForgeBuilder Platform (admin wizard, UI builder, component library)

#### Blue Hat (Process/Sequencing)
**Phase A:** Chassis foundation (correlation ID, HttpClient, validation, security headers, rate limiting, API versioning, IForgePagedResult)
**Phase B:** Data & Messaging (DbContext, interceptors, MassTransit, caching, IForgeContext, background services)
**Phase C:** Observability & Operations (OpenTelemetry, Vault, Grafana, docker compose, CI/CD)
**Phase D:** Developer Experience (EditorConfig, template repo, testing package, localization)
**Phase E:** Runtime Layer (auto-CRUD, metadata API, field permissions, plugin system, command sourcing)
**Phase F:** ForgeBuilder Platform (admin wizard, UI builder, component library, design system)

**Critical path:** A → B → E → F. Phases C and D can run in parallel.

---

### Phase 3: First Principles Summary

1. **Every microservice must be introspectable** -- metadata API is critical path for ForgeBuilder
2. **Chassis works without ForgeBuilder** -- metadata API is opt-in (`AddForgeMetadata()`)
3. **Configuration over code, code as escape hatch** -- at every layer
4. **Design for ejectability from day one** -- even if not built immediately
5. **Tracer 1 is mostly a ForgeBuilder problem** -- chassis already exists for minimal case

---

### Phase 4: Cross-Pollination (ABP Framework comparison)

#### ABP vs Forge -- Key Architectural Differences

- **ABP**: Full DDD framework, heavy base classes (`Entity<>`, `AggregateRoot<>`, `ApplicationService`), `AbpModule` dependency graph, 8-10 project solution, aggressive auto-discovery
- **Forge**: Thin wrappers, interfaces over base classes, `AddForge*()` extension methods, minimal ceremony, explicit registration

#### Ideas from Cross-Pollination

**[CP #1]: Auto-Controller Convention** -- NEEDS INVESTIGATION
ABP maps method name prefixes to HTTP verbs + routes. Forge could offer opt-in `[ForgeAutoApi]` without base class inheritance. Convention-based, standard ASP.NET Core underneath.

**[CP #2a]: Multi-Tenancy as Opt-In** -- DESIGN FOR IT, DON'T BUILD IT
Not a cloud product, not core. But design `IForgeContext` to carry nullable `TenantId` from day one. `AddForgeTenancy()` adds query filters + tenant resolution when needed. Don't close the door.

**[CP #5a]: Audit Diff-Only Storage** -- YES
- Store only changed properties (old/new values), not full entity snapshots
- Account for chatty interfaces -- multiple rapid saves should coalesce, not flood
- Stream to separate sink (time-series DB, append-only log, event store) rather than application database
- Opt-in per entity

**[CP #7a]: Forge Template Structure** -- YES (3 + test)
- `MyService.Api` -- controllers, startup, composition root
- `MyService.Contracts` -- DTOs, interfaces, published as NuGet for service-to-service consumers
- `MyService.Services` (or `.Application`) -- business logic, EF, no HTTP dependency
- `MyService.Tests` -- test project
Not ABP's 8 projects. Clean separation without ceremony.

**[CP #8-11]: Client Proxy / TypeScript Generation** -- NEEDS INVESTIGATION
- Generate TypeScript clients from OpenAPI spec for React frontends
- Candidates: orval (generates React Query hooks), openapi-typescript + openapi-fetch, NSwag, Kiota
- Orval + React Query could feed directly into ForgeBuilder blocks (table binds to `useGetBookList()`, form binds to `useCreateBook()`)
- `MyService.Contracts` NuGet → OpenAPI spec at build time → TS package generation in CI
- Generated client could include metadata (validation rules, display hints, field types) if metadata API exists
- Code-first (C# DTOs → OpenAPI → TS) vs contract-first (OpenAPI → both) -- code-first simpler for single-team ownership
- **Tension**: ForgeBuilder's runtime metadata overrides compile-time types. How do these interact? Needs design.

**[CP #6]: Module Lock-in Anti-Pattern** -- AVOID
ABP's `[DependsOn(typeof(...))]` creates compile-time coupling between modules. Forge's `AddForge*()` pattern is already better -- startup order matters, but no type-level coupling. **Keep this. Don't add a module abstraction.**

#### Cross-Pollination: Ecosystem Comparison (JHipster, Rails/Laravel, Spring Boot/Cloud, Strapi/Directus, Retool/Appsmith)

**Forge's unique position:** Low lock-in chassis + standalone UI output. No other tool combines both. Retool/Appsmith require their runtime. ABP/JHipster have heavy lock-in. Rails/Laravel are different language ecosystems. ForgeBuilder outputting standalone React apps to Git is the single biggest differentiator.

**[CP #12]: Runtime Log Level Control** -- YES
Spring Actuator's `POST /actuator/loggers` changes log level without restart. Implement with Serilog's `LoggingLevelSwitch`. Trivial effort, high ops value. Fold into `UseForgeLogging()`. Dev-only or auth-protected endpoint.

**[CP #13]: Build Info Endpoint** -- NOTED
`/info` endpoint: git SHA, build timestamp, package version, service name. Auto-populated at build time. Cheap.

**[CP #14]: Interface vs Display Separation (Directus pattern)** -- YES
ForgeBuilder field types need two separate registries:
- **Interface**: the edit widget (e.g., Money → currency-aware number input with symbol prefix)
- **Display**: the list/detail renderer (e.g., Money → formatted `$1,234.56`)
Map field types to edit widgets AND list/detail renderers independently. A `Phone` field's interface is a formatted input; its display is a clickable `tel:` link.

**[CP #15]: Layout System per Entity (Directus pattern)** -- YES
Not every entity list should be a table. ForgeBuilder blocks support: table, card grid, calendar, kanban, map. Configured per entity in ForgeBuilder, selectable by user at runtime. `layout: "table" | "cards" | "calendar" | "kanban" | "map"`.

**[CP #16]: Reactive Expression Bindings (Retool/Appsmith)** -- NOTED
ForgeBuilder block config supports `{{ }}` expressions for dynamic values. Column visibility: `{{ currentUser.roles.includes('admin') }}`. Field default: `{{ new Date().toISOString() }}`. Core UX abstraction for low-code.

**[CP #17]: Named Queries as First-Class Objects (Retool)** -- NOTED
Queries are top-level named entities in ForgeBuilder config (`GetOrders`, `UpdateStatus`), not buried in components. Table binds to `GetOrders`, form submit triggers `UpdateStatus`.

**[CP #18]: JDL-like Entity DSL** -- NOTED
JHipster's JDL defines entities, relationships, validation, pagination in one compact file. ForgeBuilder could use similar DSL or C# source generators reading attributes. Needs investigation.

**[CP #19]: Standalone Output as Differentiator** -- CONFIRMED
Retool/Appsmith lock apps inside their platform. ForgeBuilder outputs standalone React apps to Git. No runtime dependency. Eject is built-in by default. Front-and-center in positioning.

**[CP #20]: Config Gap Detection** -- YES
Auto-register health checks when Forge packages add services (e.g., `AddForgeDbContext()` → DB health check appears). Log warnings at startup when coverage is incomplete: rate limiter configured but no policy covers `/api/auth/login`, DbContext registered but no health check. Smart defaults with safety net.

**[CP #21]: Scaffold Command** -- NOTED
`dotnet forge scaffold Order Name:string Total:decimal Status:OrderStatus` emits entity, DTO, controller, migration, test. CLI for the chassis, ships with template repo.

**[CP #22]: Event Wiring Model (Retool/Appsmith)** -- NOTED
ForgeBuilder block events (onClick, onRowSelect, onSubmit) trigger actions declaratively in config: `{ "event": "onRowSelect", "action": "navigate", "target": "/orders/{{ selectedRow.id }}" }`.

---

### Phase 5: Question Storming

#### Answered / Confirmed

- **Every Forge package is independently optional.** No package assumes the full stack is present.
- **ForgeBuilder user is a developer**, can be simplified for power users later.
- **ForgeBuilder is visual** -- WYSIWYG like VB6 for web. Preview before commit.
- **Backend is hand-coded C#**, scaffolding assists but no backend builder.
- **Custom components are plain React** -- users add their own components when blocks aren't enough.
- **Elevator pitch:** Do the easy things automatically so you can focus on essential complexity instead of accidental complexity.
- **Packages are opt-in** -- teams can swap any library (Serilog, MassTransit, Keycloak) by not using that Forge package.
- **Docs:** Per-package README.
- **`IAuditable` in background jobs:** Dummy/system account when no `ICurrentUser` available.
- **One DbContext per microservice.** Two contexts = two services.
- **Soft delete query bypass:** Developers need all three (admin view, audit trail, undelete) -- provide `IgnoreQueryFilters()` guidance or helper.
- **No backporting.** Only forward versions. No other consumers yet.
- **Real-world testing:** Rebuild one existing app + one new app on Forge.
- **Metadata API:** Single source of truth, no runtime overrides conflicting with compile-time types. To be analyzed.

#### Open Questions to Investigate

**[Q #1]: Forge Package Version Conflicts**
What happens when a consuming app needs a Forge package version that conflicts with another Forge package's dependency? Coordinated upgrade path or independent versioning?

**[Q #2]: MassTransit Swap-ability**
Integration is "thin wrapper, no abstraction" -- but can a team switch to NServiceBus or Azure Service Bus? Is the wrapper thin enough? Needs investigation.

**[Q #3]: Grafana Dashboard Maintenance**
Pre-built dashboards ship with the package -- but who maintains them as metrics evolve? Versioned with NuGet? How do teams customize without losing upstream updates? Migration burden.

**[Q #4]: ForgeBuilder Concurrent Editing**
If two people edit the same ForgeBuilder app simultaneously -- is there conflict resolution in the builder itself, or only at Git merge time?

**[Q #5]: Cross-Microservice Navigation in Shell**
Multiple microservice frontends lazy-loaded in one shell. How does cross-microservice navigation work? Shared routing config? Shared state (auth, notifications)?

**[Q #6]: ForgeBuilder Config Interpreter = Runtime Dependency?**
ForgeBuilder outputs config. The deployed app interprets it. That interpreter IS a runtime dependency. How is this different from Retool's lock-in? Eject story is the answer, but the interpreter itself needs to be analyzed -- is it a thin reader or a full framework?

**[Q #7]: Metadata API -- Single Source of Truth Design**
Compile-time C# types carry semantic meaning (ValueTypes). ForgeBuilder needs field configuration (labels, visibility, ordering). How do these merge into one metadata API without two sources of truth? Needs architecture.

---

## Next Step

**Phase 4: Solution Matrix** -- prioritized roadmap with effort/value/dependency grid. To be done in a fresh context window.

---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 1
research_type: 'domain'
research_topic: '.NET application scaffolding and bootstrap frameworks'
research_goals: 'Determine what existing .NET frameworks/libraries provide opinionated scaffolding with cross-cutting concerns (config, logging, swagger, health checks, security) and where Itenium.Forge fits in this landscape'
user_name: 'Wouter'
date: '2026-02-19'
web_research_enabled: true
source_verification: true
---

# The Missing .NET Microservice Chassis: Comprehensive Domain Research on .NET Application Scaffolding Frameworks

**Date:** 2026-02-19
**Author:** Wouter
**Research Type:** Domain / Industry Analysis

---

## Executive Summary

The .NET application scaffolding space is fragmented with no single dominant solution. Existing approaches fall into five categories: full frameworks (ABP, ServiceStack), architecture templates (Clean Architecture, FullStackHero), orchestration tools (.NET Aspire), endpoint frameworks (FastEndpoints), and single-concern libraries (Coravel, Wolverine). **The "thin composable NuGet wrappers for cross-cutting concerns" niche -- the Microservice Chassis pattern -- has no widely-adopted .NET implementation.** This is the gap Itenium.Forge occupies.

The pattern is well-documented (Manning's "Microservices in .NET" Ch. 11, microservices.io) and has canonical implementations in Java (Spring Boot/Cloud) and Go (Go kit), but not in .NET. The closest public example is Corvus.* by Endjin, with very limited adoption.

**Key Findings:**

- **.NET is thriving**: C# won TIOBE Language of the Year 2025; 6-8M developers worldwide; .NET 10 LTS released Nov 2025
- **ABP is the closest competitor** but is criticized as too complex, too opinionated, hard to adopt incrementally. Forge's thin wrapper approach directly addresses ABP's #1 weakness.
- **Aspire is complementary, not competing**: handles OTel/resilience/orchestration; deliberately leaves auth, CORS, Swagger, ProblemDetails to each service
- **Templates can't upgrade**: Clean Architecture and FullStackHero generate code once -- fork-and-diverge problem. Forge's NuGet delivery is structurally superior for multi-service teams.
- **Swashbuckle is being replaced** by built-in `Microsoft.AspNetCore.OpenApi` + Scalar UI in .NET 10 (action needed)
- **EU Cyber Resilience Act** mandates SBOM by Dec 2027; SBOM generation should be added to CI
- **AI coding assistants need golden paths** -- AGENTS.md is an emerging standard; Forge should ship context files

**Strategic Recommendations:**

1. Migrate OpenAPI from Swashbuckle to built-in Microsoft pipeline + Scalar (issue #7)
2. Ship AGENTS.md for AI assistant context (issue #8)
3. Add SBOM generation to CI (issue #6)
4. Investigate OTel as option alongside Serilog (issue #5)
5. Reserve `Itenium.Forge` NuGet prefix; adopt Trusted Publishing

## Table of Contents

1. [Domain Research Scope Confirmation](#domain-research-scope-confirmation)
2. [Industry Analysis](#industry-analysis) -- Market size, dynamics, segmentation, trends, competitive overview
3. [Competitive Landscape](#competitive-landscape) -- Key players, positioning, strategies, feature coverage
4. [Regulatory Requirements](#regulatory-requirements) -- Licensing, security standards, data protection, NuGet publishing
5. [Technical Trends and Innovation](#technical-trends-and-innovation) -- AI, Aspire, .NET 10/11, architecture patterns
6. [Recommendations](#recommendations) -- Technology adoption strategy, innovation roadmap, risk mitigation
7. [Research Conclusion](#research-conclusion)

## Research Introduction

In February 2026, the question "does this already exist?" is the right one to ask before investing further in Itenium.Forge. Every hour spent building something that already exists is an hour not spent on differentiation.

This research surveyed the entire .NET scaffolding landscape using current web data -- NuGet download stats, GitHub stars, official documentation, developer sentiment (Reddit, Stack Overflow, blog posts), regulatory developments, and technology roadmaps. All factual claims are cited with sources verified in February 2026.

**Research Methodology:**
- Multi-source web research with URL citations for all claims
- NuGet and GitHub metrics verified directly
- Developer sentiment gathered from community discussions and reviews
- Regulatory analysis based on official government/standards body sources
- Parallel research agents for comprehensive coverage

**Research Goal:** Determine what existing .NET frameworks/libraries provide opinionated scaffolding with cross-cutting concerns and where Itenium.Forge fits in the landscape.

**Conclusion (preview):** Forge fills a genuine gap. The Microservice Chassis pattern has no canonical .NET implementation. Forge should stay thin, stay complementary to Aspire, and invest in the items above to establish itself.

## Domain Research Scope Confirmation

**Research Topic:** .NET application scaffolding and bootstrap frameworks
**Research Goals:** Determine what existing .NET frameworks/libraries provide opinionated scaffolding with cross-cutting concerns (config, logging, swagger, health checks, security) and where Itenium.Forge fits in this landscape

**Domain Research Scope:**

- Landscape Analysis - existing frameworks, libraries, templates in .NET ecosystem
- Feature Coverage - how each handles settings, logging, Swagger, health checks, security, CORS, ProblemDetails
- Architecture Patterns - NuGet meta-packages vs templates vs code generators
- Ecosystem & Community - maturity, adoption, NuGet stats, GitHub activity
- Gap Analysis - where Forge's thin opinionated wrapper approach is unique vs overlapping

**Research Methodology:**

- All claims verified against current public sources (NuGet, GitHub, official docs)
- Multi-source validation for adoption/popularity claims
- Confidence level framework for uncertain information
- Focused on .NET ecosystem specifically

**Scope Confirmed:** 2026-02-19

## Industry Analysis

### Market Size and Valuation

The broader **Software Development Tools market** is valued at **USD 6.4-7.6 billion in 2025**, projected to reach **USD 13.7-29.6 billion by 2030-2035** depending on the source, with a **CAGR of 14.5-17.5%**.

The .NET ecosystem specifically:
- **6-8 million .NET developers worldwide** (Softacom)
- **25.2% of software developers** use .NET (5+); **~34% of websites/web apps** run on .NET (Softacom)
- **ASP.NET Core at 19.1% usage** among professional developers in Stack Overflow 2025 survey

_Total Market Size: USD 6.4-7.6B (dev tools); .NET represents a significant slice given 6-8M developer base_
_Growth Rate: 14.5-17.5% CAGR_
_Source: [Mordor Intelligence](https://www.mordorintelligence.com/industry-reports/software-development-tools-market), [Business Research Insights](https://www.businessresearchinsights.com/market-reports/software-development-tools-market-106006), [Softacom](https://www.softacom.com/wiki/development/future-of-dot-net/)_

### Market Dynamics and Growth

**Growth Drivers:**
- **C# won TIOBE Language of the Year 2025** -- rose +2.94pp to 7.39%, ranking 5th globally. Cited as having "removed every reason why not to use C# instead of Java: cross-platform, open source, all new language features" ([TIOBE](https://www.tiobe.com/tiobe-index/), [Visual Studio Magazine](https://visualstudiomagazine.com/articles/2026/01/08/csharp-grabs-language-of-the-year-tiobe-predicts-typescript-rise.aspx))
- **.NET 10 LTS** released November 2025, supported through 2028. Large regulated enterprises expected to pilot migrations in 2026 ([Microsoft .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/))
- **AI-driven productivity**: 84% of developers use AI tools; AI writes 41% of code; developers report 25-39% productivity gains ([Index.dev](https://www.index.dev/blog/developer-productivity-statistics-with-ai-tools))
- **Full-stack framework consolidation**: developers prefer integrated ecosystems over assembling pieces ([Stack Overflow 2025](https://survey.stackoverflow.co/2025/))

**Growth Barriers:**
- Framework fatigue -- developers hesitate to adopt yet another opinionated layer
- Lock-in concerns with full frameworks (ABP, ServiceStack)
- Microsoft's own Aspire is absorbing some of the "bootstrap" surface area

_Source: [TIOBE](https://www.tiobe.com/tiobe-index/), [Stack Overflow 2025](https://survey.stackoverflow.co/2025/), [Index.dev](https://www.index.dev/blog/developer-productivity-statistics-with-ai-tools)_

### Market Structure and Segmentation

The .NET scaffolding space segments into **five distinct categories**:

| Category | Approach | Examples | Update Model |
|---|---|---|---|
| **Full Frameworks** | Build *inside* the framework; prescribes architecture, modules, conventions | ABP Framework, ServiceStack | NuGet packages (living dependency) |
| **Endpoint Frameworks** | Replace MVC/Minimal APIs with alternative endpoint model | FastEndpoints, Carter | NuGet packages |
| **Orchestration Tools** | Dev-time service composition and observability | .NET Aspire | NuGet + tooling |
| **Architecture Templates** | One-time project scaffold via `dotnet new` | Jason Taylor CA, Ardalis CA, FullStackHero | Forked code (point-in-time) |
| **Infrastructure Libraries** | Single-concern building blocks | Coravel (scheduling), MediatR (mediator), Serilog | NuGet packages |

**Key distinction:** Templates generate code once and you own it (no updates). NuGet packages are living dependencies that receive updates. Full frameworks provide everything but create vendor lock-in. **The "thin composable NuGet wrappers" niche -- where Forge sits -- is notably underserved.**

_Source: [StarterIndex](https://starterindex.com/net-boilerplates), [dotnetnew.azurewebsites.net](https://dotnetnew.azurewebsites.net/), multiple GitHub repos_

### Industry Trends and Evolution

**Emerging Trends:**
- **.NET Aspire** is Microsoft's strategic bet for cloud-native .NET. Aspire 13 shipped with .NET 10; roadmap includes CLI, single-file AppHost, VS Code extension, MCP server in dashboard, LLM-specific metrics ([GitHub Aspire Roadmap](https://github.com/dotnet/aspire/discussions/10644))
- **AI + Platform Engineering convergence** is the dominant 2026 theme ([The New Stack](https://thenewstack.io/in-2026-ai-is-merging-with-platform-engineering-are-you-ready/))
- **MediatR went commercial** (2025) -- pushing teams toward alternatives like Wolverine or source-generated mediators ([Jimmy Bogard](https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/))
- **`dotnet scaffold`** -- Microsoft's new interactive scaffolding tool for MVC, Razor Pages, EF Core (12.7K NuGet downloads, very early) ([Microsoft DevBlogs](https://devblogs.microsoft.com/dotnet/introducing-dotnet-scaffold/))

**Technology Integration:**
- Aspire ServiceDefaults already wires OpenTelemetry + health checks + resilience -- this is becoming the expected baseline for new .NET apps
- NativeAOT improvements in .NET 10 reduce startup time, making lightweight bootstrap wrappers more attractive for serverless/container scenarios

**Future Outlook:**
- Success in this space in 2026 hinges on: opinionated-but-flexible defaults, Aspire compatibility, AI-assisted development support, minimal boilerplate for the .NET 10 LTS wave
- The 4,339 `dotnet new` templates (307M total downloads) show massive demand for project bootstrapping ([dotnetnew](https://dotnetnew.azurewebsites.net/))

_Source: [GitHub Aspire Roadmap](https://github.com/dotnet/aspire/discussions/10644), [Microsoft DevBlogs](https://devblogs.microsoft.com/dotnet/introducing-dotnet-scaffold/), [Softacom](https://www.softacom.com/wiki/development/future-of-dot-net/)_

### Competitive Dynamics

**Key Players by Adoption:**

| Framework | GitHub Stars | NuGet Downloads | License |
|---|---|---|---|
| ABP Framework | 14,000 | 39.5M (core) | LGPL + Commercial ($2,999-$9,999/yr) |
| Jason Taylor Clean Arch | 19,600 | 234K | MIT |
| Ardalis Clean Arch | 18,000 | N/A | MIT |
| FullStackHero | 6,300 | N/A (template) | MIT |
| FastEndpoints | 5,500 | 9.8M | MIT |
| ServiceStack | 5,500 | 17.7M | AGPL / Commercial |
| .NET Aspire | 4,600 | 13.7M (hosting) | MIT (Microsoft) |
| Coravel | 3,800 | 6.9M | MIT + Pro |
| Dotnet-Boxed | 3,400 | N/A | MIT (less active since 2022) |
| Carter | 2,300 | 2.9M | MIT |
| Wolverine | 2,000 | 1.5M | MIT |

_Market Concentration: Fragmented -- no single dominant solution. ABP is the largest full-framework player; Clean Architecture templates dominate the one-time scaffold space._
_Barriers to Entry: Low technically, but community trust and documentation quality are the real moats._
_Innovation Pressure: High -- Microsoft's Aspire is absorbing surface area; MediatR's licensing shift is reshuffling dependencies._

_Source: GitHub repositories and NuGet package pages for each framework (verified Feb 2026)_

## Competitive Landscape

### Key Players and Market Leaders

The .NET scaffolding space has no single dominant player. Instead, competitors cluster into distinct categories:

**Tier 1 -- Full Frameworks (build *inside* the framework):**

| Player | Stars | NuGet DLs | License | Target |
|---|---|---|---|---|
| **ABP Framework** (Volosoft, Istanbul) | 14K | 39.5M (core) | LGPL + Commercial ($2,999-$9,999/yr) | Enterprise LOB/SaaS |
| **ServiceStack** (Demis Bellot) | 5.5K | 75.8M (Text pkg) | AGPL + Commercial ($299-$999/dev/yr) | Small-to-medium API teams |

**Tier 2 -- Architecture Templates (one-time scaffold):**

| Player | Stars | NuGet DLs | License |
|---|---|---|---|
| **Jason Taylor Clean Architecture** | 19.6K | 234K | MIT |
| **Ardalis Clean Architecture** | 18K | N/A | MIT |
| **FullStackHero Starter Kit** | 6.3K | N/A (template) | MIT |
| **Dotnet-Boxed** | 3.4K | N/A | MIT (less active since 2022) |

**Tier 3 -- Microsoft Official:**

| Player | Stars | Status |
|---|---|---|
| **.NET Aspire** (ServiceDefaults) | 4.6K | Aspire 13 shipped Nov 2025; 142 integrations |
| **`dotnet scaffold`** | N/A | 12.7K NuGet DLs, very early |
| **`dotnet new` templates** | N/A | 4,339 templates, 307M total downloads |

**Tier 4 -- Single-Concern Libraries:**

| Player | Stars | NuGet DLs | Concern |
|---|---|---|---|
| **FastEndpoints** | 5.5K | 9.8M | Endpoint framework |
| **Carter** | 2.3K | 2.9M | Endpoint modules |
| **Coravel** | 3.8K | 6.9M | Scheduling, queuing, events |
| **Wolverine** | 2K | 1.5M | Mediator + message bus |

**Tier 5 -- Internal Platform Packages (Forge's category):**

| Player | Status |
|---|---|
| **Corvus.*** (Endjin) | Real consulting company publishing opinionated NuGet wrappers -- closest public example to Forge. Limited adoption outside Endjin's own projects |
| **"Microservices.Platform" pattern** | Documented in blog posts and books, but no dominant OSS implementation |
| **Forge** | This project |

_Source: [ABP.IO](https://abp.io/), [ServiceStack](https://servicestack.net/), [GitHub repos](https://github.com/), [StarterIndex](https://starterindex.com/net-boilerplates), [Corvus -- endjin](https://endjin.com/blog/2022/11/an-overview-of-the-corvus-extensions-library)_

### Market Share and Competitive Positioning

No formal market share data exists for this niche. Proxy metrics from NuGet/GitHub:

**By adoption (NuGet downloads):** ABP leads in the full-framework space (39.5M core downloads, 14.3K/day). FastEndpoints is growing fast (9.8M, 15.2K/day). Aspire.Hosting shows strong Microsoft-backed momentum (13.7M, 28.1K/day).

**By mindshare (GitHub stars):** Clean Architecture templates dominate (Jason Taylor 19.6K, Ardalis 18K) -- indicating developers want *architectural guidance* more than runtime frameworks.

**Positioning map:**

```
                    Thin / Composable
                         |
              Coravel    |    Forge (here)
              Carter     |    Aspire ServiceDefaults
                         |    Corvus.*
    Single-concern ------+------ Full cross-cutting
                         |
              FastEndpoints    FullStackHero
              Wolverine  |    ABP Framework
                         |    ServiceStack
                         |
                    Heavy / All-in-one
```

Forge occupies the **top-right quadrant**: thin/composable wrappers covering full cross-cutting concerns. This quadrant is largely empty in the OSS .NET world.

_Source: NuGet download stats, GitHub star counts (verified Feb 2026)_

### Competitive Strategies and Differentiation

**ABP Framework -- Platform lock-in strategy:**
- Comprehensive module ecosystem (27+ modules: identity, multi-tenancy, audit, SaaS, file management, chat, GDPR, payment, workflows)
- ABP Suite code generator creates full CRUD vertical slices (entity, repo, service, migration, UI pages) in seconds
- ABP Conference, training ($899/class), "Mastering ABP" e-book -- building a knowledge moat
- Customer case studies: Standard Chartered Bank (SC Ventures), Aon (70% faster dev), FPT Software
- **Weakness exploitable by Forge:** "Ultra complex", steep learning curve, difficult incremental adoption, upgrade pain between major versions. Developers building within ABP's boundaries, not alongside them.

**ServiceStack -- Developer productivity via conventions:**
- AutoQuery: define a Request DTO, get a queryable REST API automatically -- unique differentiator
- End-to-end typed clients for 10 languages (C#, TS, Swift, Java, Kotlin, Dart, Python, F#, VB.NET)
- AI-first: templates ship with `AGENTS.md` for AI code generation context
- **Weakness exploitable by Forge:** Small community (~107 tracked companies), bus factor (single primary maintainer), per-developer annual cost, custom middleware stack diverges from standard ASP.NET Core

**Clean Architecture templates -- Architectural guidance:**
- Massive GitHub following proves demand for opinionated project structure
- One-time scaffold -- **no upgrade path**. Once you modify the code, you own it.
- **Weakness exploitable by Forge:** Templates become stale. When .NET 10 ships, you manually migrate. With NuGet packages, you `dotnet nuget update`.

**.NET Aspire -- Microsoft ecosystem play:**
- Integrated into VS, .NET CLI, Azure. Heavy Microsoft push.
- Deliberately scoped to orchestration + observability. Leaves application concerns to you.
- **Not a competitor to Forge -- a complement.** Aspire ServiceDefaults covers OTel + health + resilience. Forge covers auth + CORS + Swagger + ProblemDetails + Serilog.

_Source: [ABP Pricing](https://abp.io/pricing), [ABP Customers](https://abp.io/customers/success-stories), [ServiceStack AutoQuery](https://servicestack.net/autoquery), [ServiceStack v10 Release](https://docs.servicestack.net/releases/v10_00), [Aspire ServiceDefaults](https://aspire.dev/fundamentals/service-defaults/)_

### Business Models and Value Propositions

| Player | Model | Revenue Stream | Value Proposition |
|---|---|---|---|
| **ABP** | Open core + commercial tiers | $2,999-$9,999/yr per 3 devs; training $899/class | "Don't reinvent enterprise plumbing -- modules for everything" |
| **ServiceStack** | Per-developer subscription | $299-$999/dev/yr; volume discounts at 5+ | "Productive API development with typed clients + AutoQuery" |
| **FullStackHero** | Pure OSS (MIT) | None (community goodwill) | "Production-grade starter saving 200+ dev hours" |
| **FastEndpoints** | Pure OSS (MIT) | None | "Developer-friendly alternative to Minimal APIs & MVC" |
| **Aspire** | Microsoft-backed OSS (MIT) | Drives Azure adoption | "Cloud-native orchestration for local dev" |
| **Forge** (potential) | Internal tooling / OSS | Competitive edge for consultancy (cheaper/faster project delivery) | "Thin wrappers, NuGet-upgradable, standard ASP.NET Core underneath" |

_Source: [ABP Pricing](https://abp.io/pricing), [ServiceStack Pricing](https://account.servicestack.net/pricing), [FullStackHero GitHub](https://github.com/fullstackhero/dotnet-starter-kit)_

### Competitive Dynamics and Entry Barriers

**Barriers to entry (for Forge):**
- **Technical barriers: Low.** The pattern is well-understood. Extension methods on `WebApplicationBuilder` are straightforward to write.
- **Community/trust barriers: High.** Developers adopt scaffolding frameworks based on documentation quality, GitHub stars, blog posts, conference talks, and "someone I trust uses it." Building this takes years.
- **Switching costs for incumbents: Varies.** ABP lock-in is high (your entire architecture depends on it). Clean Architecture templates have zero lock-in (forked code). Forge's thin wrapper approach should have low switching costs by design.

**Developer sentiment trend:**
Multiple sources indicate .NET developers increasingly want **lighter, less opinionated scaffolding**. The sentiment is: "give me the patterns and infrastructure, but don't own my architecture." Clean Architecture templates, Minimal APIs, Aspire, and lightweight generators (Nano ASP.NET Boilerplate) are gaining traction as alternatives to ABP and ServiceStack.

**The "Microservice Chassis" pattern validation:**
Forge implements the [Microservice Chassis pattern](https://microservices.io/patterns/microservice-chassis.html) -- a well-documented architectural pattern. Canonical examples exist in Java (Spring Boot/Spring Cloud) and Go (Go kit). **There is no widely-adopted canonical .NET equivalent.** This is the gap. Manning's "Microservices in .NET" dedicates an entire chapter (Ch. 11) to building reusable microservice platforms from NuGet packages -- describing exactly what Forge does.

_Source: [microservices.io -- Chassis pattern](https://microservices.io/patterns/microservice-chassis.html), [Manning -- Microservices in .NET Ch. 11](https://livebook.manning.com/book/microservices-in-net-core-second-edition/chapter-11/v-8), [CODE Magazine -- Efficient Microservice Dev](https://www.codemag.com/Article/2109061/Efficient-Microservice-Development-with-.NET-5), [Top ABP Alternatives 2026](https://www.brickstarter.net/blog/post/top-abp-framework-alternatives)_

### Ecosystem and Feature Coverage Comparison

| Concern | Aspire ServiceDefaults | FullStackHero | ABP Framework | ServiceStack | **Forge** |
|---|---|---|---|---|---|
| **Delivery** | Source template | `dotnet new` fork | NuGet (framework) | NuGet (framework) | **NuGet (thin wrappers)** |
| **Upgrade path** | Manual (your code) | Git merge conflicts | `dotnet update` | `dotnet update` | **`dotnet update`** |
| **Auth (JWT/OIDC)** | No | Yes (Identity) | Yes (full identity system) | Yes (multi-provider) | **Yes (Keycloak + OpenIddict)** |
| **CORS** | No | Yes | Yes | Yes | **Yes** |
| **Swagger/OpenAPI** | No | Yes | Yes | Yes (custom metadata) | **Yes** |
| **ProblemDetails** | No | Yes (exception handling) | Yes | No (custom error handling) | **Yes (RFC 7807)** |
| **Structured logging** | OTLP only | Serilog | ABP logging | Built-in | **Serilog + Loki** |
| **Health checks** | Yes (basic) | Yes | Yes | No | **Yes** |
| **OpenTelemetry** | Yes (full) | Yes (via Aspire) | Partial | No | No |
| **HTTP resilience** | Yes (Polly) | No | No | No | No |
| **Multi-tenancy** | No | Yes (Finbuckle) | Yes (built-in) | Via annotations | No |
| **Settings/config** | No | Yes | Yes (settings module) | No | **Yes (appsettings loading)** |
| **Lock-in level** | Low | Medium (fork diverge) | High | High | **Low** |
| **Architecture ownership** | None | Full (Clean Arch) | Full (DDD + modules) | Full (DTO-first) | **None (your code)** |

## Regulatory Requirements

### Applicable Regulations

For a .NET scaffolding framework / NuGet package ecosystem, traditional industry regulations don't directly apply. Instead, the regulatory landscape covers **open-source licensing, software supply chain security, and data protection obligations that the framework helps its consumers satisfy.**

**Key regulatory instruments:**

| Regulation | Scope | Impact on Forge |
|---|---|---|
| **EU Cyber Resilience Act (2024/2847)** | All "products with digital elements" sold in EU | SBOM mandatory; vulnerability reporting by **Sep 11, 2026**; full compliance by **Dec 11, 2027**. Penalties: up to 15M EUR / 2.5% turnover |
| **US EO 14028 + EO 14144** | Software sold to US federal agencies | Machine-readable SBOMs required; SSDLC attestations |
| **GDPR Article 25** | Any app processing EU personal data | "Data protection by design" -- frameworks should provide auth, audit trails, data export/deletion foundations |
| **CCPA (updated Jan 2026)** | Businesses with >$25M revenue processing CA consumer data | Similar to GDPR: data access, deletion, consent management |

_Source: [EU CRA (SafeDep)](https://safedep.io/sbom-and-eu-cra-cyber-resilience-act/), [CISA SBOM 2025](https://www.cisa.gov/resources-tools/resources/2025-minimum-elements-software-bill-materials-sbom), [GDPR Article 25](https://gdpr-info.eu/art-25-gdpr/), [CCPA 2026 (Jackson Lewis)](https://www.jacksonlewis.com/insights/navigating-california-consumer-privacy-act-30-essential-faqs-covered-businesses-including-clarifying-regulations-effective-1126)_

### Industry Standards and Best Practices

**De facto standards relevant to Forge's feature set:**

| Standard | Status | Forge Coverage |
|---|---|---|
| **RFC 9457** (ProblemDetails, obsoletes RFC 7807) | De facto standard for HTTP API errors | Forge provides `AddForgeProblemDetails()` / `UseForgeProblemDetails()` |
| **OpenAPI 3.2.0** | Industry standard for API documentation; increasingly treated as governance/compliance artifact in regulated industries | Forge provides `AddForgeSwagger()` / `UseForgeSwagger()` |
| **OpenTelemetry** | CNCF graduated project; de facto observability standard | Forge uses Serilog + Loki; OTel is a gap/opportunity |
| **Kubernetes health probes** | De facto standard for container orchestration (liveness/readiness/startup) | Forge provides `AddForgeHealthChecks()` / `UseForgeHealthChecks()` with `/health/live` and `/health/ready` |
| **OWASP API Security Top 10 (2023)** | Industry security guideline | Forge addresses: Broken Auth (#2), Security Misconfiguration (#8) via security + CORS + ProblemDetails modules |

No formal RFC exists for health check response format (draft-inadarei-api-health-check-06 is dormant). Kubernetes probe conventions are the de facto standard.

_Source: [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457.html), [OpenAPI 3.2.0](https://spec.openapis.org/oas/v3.2.0.html), [OWASP API Security Top 10](https://owasp.org/API-Security/editions/2023/en/0x11-t10/), [Kubernetes Probes](https://kubernetes.io/docs/concepts/configuration/liveness-readiness-startup-probes/)_

### Compliance Frameworks

**Security certifications (SOC2, ISO 27001)** are organizational-level, not framework-level. However, a scaffolding framework can help consumers achieve compliance by providing:

| SOC2 / ISO 27001 Control | How Forge Helps |
|---|---|
| CC6.1 -- Access control | `ICurrentUser`, JWT claims extraction, Keycloak/OpenIddict integration |
| CC7.1 -- System monitoring | Health checks (`/health/live`, `/health/ready`) |
| CC7.2 -- Anomaly detection | Serilog structured logging, request logging |
| A.10 -- Cryptographic controls | HTTPS enforcement via Kestrel/reverse proxy (not Forge-specific) |
| Audit trail requirements | Request logging middleware (Serilog) |

_Source: [SOC 2 vs ISO 27001 (StrongDM)](https://www.strongdm.com/blog/iso-27001-vs-soc-2), [SOC 2 vs ISO 27001 (Drata)](https://drata.com/grc-central/iso-27001/iso-27001-vs-soc-2)_

### Data Protection and Privacy

**GDPR "Data Protection by Design" (Article 25)** requires technical measures integrated at design time. For a scaffolding framework, this means providing foundations for:

- **Authentication/Authorization** -- Forge provides via Keycloak + OpenIddict modules
- **Audit logging** -- Forge provides via Serilog request logging
- **Data export/deletion** -- Not currently in Forge (ABP Commercial has a dedicated GDPR module with user data download + deletion)
- **Cookie consent** -- App-level concern, not framework-level

ASP.NET Core has built-in GDPR support (cookie consent feature, Identity management page with download/delete user data).

_Source: [ASP.NET Core GDPR](https://learn.microsoft.com/en-us/aspnet/core/security/gdpr), [ABP GDPR Module](https://abp.io/modules/Volo.Gdpr)_

### Licensing and Certification

**Open-source license implications for Forge and its consumers:**

| License | Used By | Consumer Impact |
|---|---|---|
| **MIT** | Most .NET ecosystem packages, Forge | No restrictions. Consumers can use in commercial apps freely. Safest choice. |
| **LGPL-3.0** | ABP Framework | Commercial use OK if only *using* (not modifying) the library. NuGet's reference model satisfies the "replaceable library" requirement. |
| **AGPL-3.0** | ServiceStack | Network-accessible apps must open-source entire codebase. Non-starter for commercial SaaS without purchasing commercial license. |

**Forge's MIT license is the correct strategic choice** -- zero friction for consumers.

**License compliance scanning tools for .NET:**
- `dotnet-delice` -- analyzes project dependency licenses
- `nuget-license` -- reads `project.assets.json`, supports allow/block lists
- `PackScan` -- CI/CD integration for license analysis

_Source: [LGPL on NuGet](https://licenses.nuget.org/LGPL-3.0-or-later), [ABP LGPL Implications](https://abp.io/support/questions/8621/LGPL-30-License-Implications), [AGPL non-starter (Open Core Ventures)](https://www.opencoreventures.com/blog/agpl-license-is-a-non-starter-for-most-companies)_

### Implementation Considerations

**Actionable items for Forge based on regulatory landscape:**

| Area | Priority | Action |
|---|---|---|
| **SBOM generation** | High | Add CycloneDX SBOM generation to CI. EU CRA deadline Sep 2026. Helps consumers meet their own obligations. |
| **NuGet prefix reservation** | High | Reserve `Itenium.Forge` on nuget.org. Prevents name-squatting and dependency confusion attacks. |
| **Trusted Publishing** | Medium | Adopt NuGet Trusted Publishing via GitHub Actions OIDC. Eliminates long-lived API keys. |
| **Vulnerability scanning** | Already done | .NET 10 target enables `NuGetAuditMode=all` by default. |
| **Package lock files** | Medium | Commit `packages.lock.json` in Forge's own repos and recommend consumers do the same. |
| **License scanning** | Medium | Add `nuget-license` to CI to verify all transitive deps are MIT/Apache-2.0/BSD compatible. |
| **OTel support** | Medium | Consider adding OpenTelemetry as option alongside Serilog -- it's becoming the expected baseline. |
| **OWASP documentation** | Low | Document which OWASP API Top 10 risks Forge addresses out-of-the-box. |

_Source: [CycloneDX for .NET](https://github.com/CycloneDX/cyclonedx-dotnet), [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), [NuGet ID Prefix Reservation](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation), [NuGet Security Best Practices](https://learn.microsoft.com/en-us/nuget/concepts/security-best-practices)_

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| **EU CRA SBOM non-compliance** | High (mandatory by 2027) | High (15M EUR penalty for consumers) | Generate SBOM in CI; provide SBOM with each NuGet release |
| **NuGet supply chain attack** (dependency confusion, typosquatting) | Medium (active attacks in 2025) | High | Reserve prefix, enable signing, commit lock files, source mapping |
| **Transitive dependency with incompatible license** | Low | Medium (legal exposure for consumers) | License scanning in CI |
| **GDPR "design by default" gap** | Medium | Medium (consumers may need data export/deletion) | Document what Forge provides vs. what consumers must implement themselves |
| **OTel becoming mandatory baseline** | Medium (de facto standard, not de jure) | Medium (Forge perceived as behind) | Plan OTel integration as optional module |

## Technical Trends and Innovation

### Emerging Technologies

**1. AI-Assisted Development is evolving scaffolding, not replacing it**

AI coding assistants (Copilot, Claude Code) can now scaffold entire projects from natural language. However, rather than making scaffolding frameworks obsolete, AI needs well-defined "golden path" templates and context files to generate correct, consistent code. The framework becomes the *source of truth* that AI consumes.

Key developments:
- **AGENTS.md** is an emerging open standard (Linux Foundation's Agentic AI Foundation) adopted by 60K+ OSS projects. ServiceStack already ships it in all .NET templates. Forge should consider shipping one too.
- **GitHub Copilot** has agentic capabilities: scaffolding projects, multi-file edits, running tests. A dedicated ASP.NET Core modernization agent exists.
- **Claude Code + C#**: C#'s strong typing produces dramatically more reliable AI output vs dynamic languages. Community `dotnet-skills` plugin provides 30 skills for C#, Aspire, EF Core, testing.
- **ABP's AI Management Module** (preview): configure AI workspaces, swap providers (OpenAI/Azure/Ollama), adjust prompts -- all without redeployment.

_Source: [AGENTS.md standard (InfoQ)](https://www.infoq.com/news/2025/08/agents-md/), [Claude Code for C# (zenvanriel)](https://zenvanriel.nl/ai-engineer-blog/claude-code-csharp-dotnet-developers/), [ServiceStack vibe templates](https://servicestack.net/posts/vibecode-react-templates), [ABP AI Module](https://abp.io/community/articles/introducing-the-ai-management-module-nz9404a9)_

**2. .NET 10 built-in OpenAPI replaces Swashbuckle**

`Microsoft.AspNetCore.OpenApi` is now the recommended path for API documentation in .NET 10. AOT-compatible, OpenAPI 3.1 support. **Scalar** has emerged as the modern UI replacement for Swagger UI (faster, better UX, smart request builder). Swashbuckle still works but is the legacy path.

*Impact on Forge:* `AddForgeSwagger()` may need rethinking -- consider renaming to `AddForgeOpenApi()` and supporting both Swashbuckle (legacy) and Scalar (modern) or just the built-in Microsoft pipeline.

_Source: [Swagger is Dead? (codewithmukesh)](https://codewithmukesh.com/blog/dotnet-swagger-alternatives-openapi/), [.NET 10 OpenAPI Scalar (ServiceStack)](https://servicestack.net/posts/openapi-net10)_

**3. NativeAOT for Web APIs is production-ready**

.NET 10 NativeAOT: startup 14ms (down from 70ms), 50%+ memory reduction. Minimal APIs are specifically optimized for it. Key limitation: ORMs using unbound reflection (EF Core) and serializers (Newtonsoft) aren't compatible. Pushes framework design toward source-generator-based approaches.

_Source: [Ultra-Fast APIs with .NET 10 NativeAOT (C# Corner)](https://www.c-sharpcorner.com/article/building-ultra-fast-apis-with-net-10-and-native-aot/), [State of NativeAOT .NET 10](https://code.soundaranbu.com/state-of-nativeaot-net10)_

**4. MCP (Model Context Protocol) as first-class .NET concern**

MCP C# SDK enables building MCP servers/clients. ASP.NET Core APIs can be annotated with MCP to become AI agent tools. Aspire Dashboard has an MCP server for AI assistants to query live telemetry. **.NET 11 roadmap explicitly prioritizes "building agentic web apps" and MCP support.**

_Source: [Build MCP server in C# (Microsoft)](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/), [.NET AI + MCP (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)_

### Digital Transformation

**Aspire is absorbing bootstrap surface area**

Aspire continues expanding its scope with each release:
- **Aspire 9.5**: Single-file AppHost (no `.csproj` needed) -- dramatically lowers bootstrapping bar
- **Aspire 13**: Dropped ".NET" prefix (polyglot: Python, JS); 142 integrations; `aspire do` reimagines build/publish/deploy; MCP server in dashboard
- **Aspire CLI**: `aspire update` (auto-detect/update packages), `aspire mcp init`, built-in runtime acquisition (node, python, .NET, java)
- **2026 roadmap**: Multi-repo support, VS Code extension, continued integration expansion

Aspire's ServiceDefaults handle OTel + health + resilience. It deliberately does NOT cover auth, Swagger/OpenAPI, CORS, ProblemDetails, Serilog. **This leaves Forge's core value proposition intact** -- but Forge should ensure it works cleanly alongside Aspire rather than conflicting.

_Source: [Aspire 9.5 (Microsoft)](https://devblogs.microsoft.com/dotnet/announcing-dotnet-aspire-95/), [Aspire 13 (Microsoft)](https://devblogs.microsoft.com/aspire/aspire13/), [Aspire Roadmap](https://github.com/dotnet/aspire/discussions/10644)_

**Platform Engineering + Golden Paths**

Platform teams in 2026 define "golden paths" -- standardized, opinionated routes where security gates are mandatory and everything else is designed to make the right thing easiest. Forge is exactly this pattern: a golden path for .NET API cross-cutting concerns. Gartner expects 40% of software engineering orgs to have InnerSource programs by 2026.

_Source: [Platform Engineering Predictions 2026](https://platformengineering.org/blog/10-platform-engineering-predictions-for-2026), [Golden Paths (Jellyfish)](https://jellyfish.co/library/platform-engineering/golden-paths/)_

### Innovation Patterns

**Architecture trends reshaping how frameworks are consumed:**

| Pattern | Status | Impact on Forge |
|---|---|---|
| **Vertical Slice Architecture** | Gaining momentum as alternative to Clean Architecture. .NET 10/C# 14 make slices cleaner. Pragmatic recommendation: hybrid Clean + Vertical Slices. | Forge is architecture-agnostic (thin wrappers), so it works with both. This is a strength vs ABP (DDD-locked). |
| **Modular Monolith** | Organizations consolidating back from microservices. Shopify as canonical example. 1-2 ops engineers vs 2-4 for microservices. | Forge's composable packages work for both monoliths and microservices. |
| **CQRS without MediatR** | MediatR went commercial. Alternatives: **Wolverine** (source-generated, free), **Cortex.Mediator** (MIT), ASP.NET Core endpoint filters, or custom 30-line implementations. | Not directly Forge's concern, but good to know for consulting advice. |
| **Event Sourcing** | No longer fringe. **Marten 8.0** (PostgreSQL) + **Wolverine** = "Critter Stack" for integrated CQRS+ES+messaging. | Potential future Forge module: `AddForgeEventSourcing()`? |

_Source: [VSA in .NET 10 (nadirbad)](https://nadirbad.dev/vertical-slice-architecture-dotnet), [Modular Monolith vs Microservices 2026](https://codingplainenglish.medium.com/why-teams-are-moving-back-from-microservices-to-modular-monoliths-in-2026-76a3eb7162b8), [MediatR Alternative: Wolverine](https://thecodeman.net/posts/mediatr-alternative-wolverine)_

### Future Outlook

**.NET 11 Preview 1** (February 2026):
- **Runtime Async** -- one of the most significant runtime changes in .NET history. Moves async understanding from compiler rewrite into the runtime. Performance improvements across entire async ecosystem.
- **.NET 11 ASP.NET Core roadmap** explicitly prioritizes "building agentic web apps" and "Copilot-assisted web development"
- **Visual Studio 2026** released as "first Intelligent Developer Environment" -- Profiler Agent, Debugger Agent, deep Copilot integration, 50% faster solution loads

_Source: [.NET 11 Preview 1 (Microsoft)](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-1/), [VS 2026 GA (Visual Studio Magazine)](https://visualstudiomagazine.com/articles/2025/11/12/visual-studio-2026-ga-first-intelligent-developer-environment-ide.aspx)_

### Implementation Opportunities

Based on technical trends, priority opportunities for Forge:

| Opportunity | Priority | Rationale |
|---|---|---|
| **Ship AGENTS.md / CLAUDE.md with Forge templates** | High | AI assistants need context about Forge's conventions to generate correct startup code. Low effort, high impact. |
| **Migrate from Swashbuckle to built-in OpenAPI + Scalar** | High | Swashbuckle is legacy path in .NET 10. Microsoft's built-in OpenAPI is AOT-compatible. |
| **Ensure Aspire compatibility** | High | Test that `AddForge*()` methods work cleanly alongside Aspire ServiceDefaults without conflicting OTel/health registrations. |
| **Add OTel option** | Medium | See GitHub issue #5. Could offer `AddForgeObservability()` alongside `AddForgeLogging()`. |
| **NativeAOT compatibility** | Medium | Verify Forge's packages work under NativeAOT. Avoid unbound reflection. |
| **MCP integration** | Low (future) | As MCP matures, consider `AddForgeMcp()` to expose app metadata to AI agents. |

### Challenges and Risks

| Challenge | Risk Level | Mitigation |
|---|---|---|
| **Aspire absorbing more surface area** | Medium | Stay complementary. Monitor Aspire releases. If Aspire adds auth/CORS/OpenAPI setup, Forge's value narrows. |
| **AI making scaffolding "unnecessary"** | Low | AI needs golden paths to generate consistent code. Forge IS the golden path. Ship context files. |
| **Swashbuckle deprecation** | High (already happening) | Migrate `AddForgeSwagger()` to built-in OpenAPI. Do this soon -- .NET 10 templates already don't include Swagger UI. |
| **NativeAOT incompatibility** | Medium | Audit Forge's dependencies for reflection usage. Serilog has AOT support; verify Keycloak/OpenIddict clients. |
| **.NET version churn** | Low | Already targeting net10.0 (LTS). .NET 11 will be STS -- decide if Forge supports both. |

## Recommendations

### Technology Adoption Strategy

1. **Immediate (next release):** Migrate OpenAPI from Swashbuckle to `Microsoft.AspNetCore.OpenApi` + Scalar UI. Ship AGENTS.md/CLAUDE.md context files.
2. **Short-term (Q2 2026):** Add SBOM generation to CI (issue #6). Reserve `Itenium.Forge` NuGet prefix. Validate Aspire ServiceDefaults compatibility.
3. **Medium-term (Q3-Q4 2026):** Investigate OTel module (issue #5). NativeAOT compatibility audit. Consider `AddForgeObservability()` as optional module.
4. **Long-term (2027+):** MCP integration. Event sourcing module. Evaluate if Aspire has absorbed enough to warrant Forge contributing upstream.

### Innovation Roadmap

Forge's strategic positioning: **the missing .NET Microservice Chassis**. No canonical OSS implementation exists in .NET (Spring Boot/Cloud fills this role in Java). Forge can own this niche by:
- Staying thin (don't become ABP)
- Staying complementary to Aspire (don't compete with Microsoft)
- Staying upgradable (NuGet > templates)
- Embracing AI context (AGENTS.md, structured conventions)

### Risk Mitigation

- **Monitor Aspire releases** quarterly for feature overlap
- **Maintain .NET LTS alignment** -- support current LTS, validate STS compatibility
- **License scanning in CI** -- verify all transitive deps remain MIT/Apache/BSD compatible
- **Community building** -- blog posts explaining the Microservice Chassis pattern and how Forge implements it. This is the #1 barrier to adoption.

## Research Conclusion

### Summary of Key Findings

**Does this already exist?** Yes and no.

- **Full frameworks** (ABP, ServiceStack) cover the same cross-cutting concerns but are heavy, opinionated, and create vendor lock-in. They own your architecture.
- **Templates** (Clean Architecture, FullStackHero) scaffold good projects but can't be upgraded via NuGet. Fork-and-diverge.
- **Aspire** handles orchestration and observability but deliberately leaves auth, CORS, Swagger, ProblemDetails, and Serilog to each service.
- **The exact niche Forge occupies -- thin, composable NuGet wrappers that configure the startup pipeline without owning your architecture -- has no widely-adopted .NET implementation.**

This niche is called the **Microservice Chassis pattern** and has canonical implementations in other ecosystems (Spring Boot/Cloud in Java, Go kit in Go). It's documented in Manning books and on microservices.io. The .NET ecosystem is missing one.

### Strategic Position

Forge's competitive advantage is precisely what it *doesn't* do:
- It doesn't own your domain model (unlike ABP)
- It doesn't fork-and-diverge (unlike templates)
- It doesn't compete with Microsoft (unlike ServiceStack)
- It complements Aspire rather than overlapping

### Immediate Action Items (GitHub Issues Created)

| Issue | Priority | Description |
|---|---|---|
| [#5](https://github.com/Itenium-Forge/Itenium.Forge.Core/issues/5) | Medium | Investigate replacing Serilog/Loki with OpenTelemetry |
| [#6](https://github.com/Itenium-Forge/Itenium.Forge.Core/issues/6) | High | Add SBOM generation to CI pipeline (EU CRA deadline Sep 2026) |
| [#7](https://github.com/Itenium-Forge/Itenium.Forge.Core/issues/7) | High | Migrate from Swashbuckle to built-in OpenAPI + Scalar UI |
| [#8](https://github.com/Itenium-Forge/Itenium.Forge.Core/issues/8) | High | Ship AGENTS.md for AI coding assistant context |

### Next Steps

1. **Execute immediate actions** -- issues #7 and #8 are low-effort, high-impact
2. **Validate Aspire compatibility** -- test `AddForge*()` alongside Aspire ServiceDefaults
3. **Reserve `Itenium.Forge` NuGet prefix** on nuget.org
4. **Consider blogging** about the Microservice Chassis pattern in .NET -- this builds credibility and positions Forge as the reference implementation

---

**Research Completion Date:** 2026-02-19
**Source Verification:** All facts cited with URLs, verified February 2026
**Confidence Level:** High -- based on multiple authoritative sources (NuGet, GitHub, Microsoft docs, industry reports)

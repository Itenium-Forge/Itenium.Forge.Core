# Forge Progress

🟢 Complete &nbsp;·&nbsp; 🔵 In Progress &nbsp;·&nbsp; ⭕ Not Started

**Effort / Complexity / Size:** 1 = Trivial · 2 = Small · 3 = Medium · 4 = Large · 5 = Extra Large

- **Effort** — how much work/time the feature requires
- **Complexity** — technical difficulty and risk
- **Size** — holistic story estimate combining effort and complexity

---

| Code | Feature | Effort | Complexity | Size | Status | Dependencies |
|------|---------|:------:|:----------:|:----:|:------:|:-------------|
| **Foundation** | | | | | | |
| F1 | Settings & Configuration | 2 | 2 | 2 | 🟢 | — |
| F2 | Logging — Serilog, request logging, Loki | 3 | 3 | 3 | 🟢 | F1 |
| F3 | Controllers — MVC, JSON, CORS, host filtering | 2 | 2 | 2 | 🟢 | F1 |
| F4 | Swagger / OpenAPI | 1 | 1 | 1 | 🟢 | F3 |
| F5 | Health Checks — liveness & readiness probes | 2 | 2 | 2 | 🟢 | F1 |
| F6 | Security Core — ICurrentUser, claims extraction | 2 | 3 | 3 | 🟢 | F1 |
| F7 | Security — Keycloak JWT Bearer | 2 | 3 | 3 | 🟢 | F6 |
| F8 | Security — OpenIddict identity server | 5 | 5 | 5 | 🟢 | F6 |
| **Security (A-series)** | | | | | | |
| A1 | Correlation ID — W3C traceparent propagation | 2 | 2 | 2 | 🟢 | F2 |
| A2 | Security Headers middleware | 2 | 2 | 2 | 🟢 | — |
| A3 | Authorization Policy Enforcement | 3 | 3 | 3 | 🔵 | F6, F7, F8 |
| **Observability (C-series)** | | | | | | |
| C1 | OpenTelemetry — tracing, metrics, OTLP, Prometheus | 4 | 4 | 4 | 🟢 | F2 |

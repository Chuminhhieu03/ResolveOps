# ResolveOps

> **Logistics Exception & Carrier Claims Management Platform**

---

## What is ResolveOps?

ResolveOps is a multi-tenant operational platform that:

- **Detects** logistics exceptions (delays, damage, partial delivery, missing documents) using deterministic versioned rules.
- **Coordinates resolution** through assigned case workflows with SLA tracking, tasks, and timeline audit.
- **Collects evidence** securely for carrier claims.
- **Manages carrier claims** from eligibility through submission, carrier decision, and financial recovery.
- **Produces intelligence** — carrier scorecards, SLA performance, and financial recovery metrics.

It sits **beside** an ERP, TMS, or carrier portal, handling the part those systems do poorly: abnormal situations requiring human coordination, deadlines, documents, decisions, and financial recovery.

---

## Non-Goals

ResolveOps is **not**:

- A Transportation Management System (TMS).
- A route optimizer or dispatch system.
- A warehouse management system (WMS).
- An ERP or accounting system.
- A driver payroll system.
- A predictive ML/AI freight forecasting system.
- A microservices architecture (MVP is a Modular Monolith).

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│  Angular 19+ SPA (Angular Material)                         │
├─────────────────────────────────────────────────────────────┤
│  ResolveOps.Api    (ASP.NET Core Minimal APIs, JWT auth)    │
│  ResolveOps.Worker (BackgroundService — outbox, consumers,  │
│                     scheduled jobs, document processing)     │
├─────────────┬───────────┬────────────┬──────────────────────┤
│  SQL Server │ RabbitMQ  │   Redis    │  MinIO (S3-compat.)  │
│     2022    │    3.x    │     7      │  (evidence storage)  │
└─────────────┴───────────┴────────────┴──────────────────────┘
                    Observed by: OpenTelemetry + Serilog + Seq
```

**Modular Monolith + Vertical Slice Architecture.** Modules: Identity, Tenancy, Partners, Shipments, Tracking, Exceptions, Workflow, Documents, Claims, Notifications, Integrations, Reporting, Audit.

See [`docs/diagrams/`](docs/diagrams/) for full C4 diagrams and sequence diagrams.

---

## Local Development

> **Prerequisites:** .NET 10 SDK, Docker Desktop, Node.js LTS, Angular CLI.

### Phase 1+ (once Aspire AppHost is configured)

```bash
# Start full local stack
dotnet run --project src/AppHost/ResolveOps.AppHost
```

This starts: SQL Server 2022, RabbitMQ 3, Redis 7, MinIO, Seq, Mailpit, API, Worker, and Aspire Dashboard.

### Build

```bash
dotnet restore ResolveOps.slnx
dotnet build ResolveOps.slnx --configuration Release
```

### Test

```bash
dotnet test ResolveOps.slnx --configuration Release
```

---

## Current Phase

**Phase 0 — Product and Repository Foundation** ✅ Complete

Next: **Phase 1 — Runtime, Aspire, database, and observability foundation**

See [`AGENTS.md`](AGENTS.md) for full phase tracking and [`CHANGELOG.md`](CHANGELOG.md) for history.

---

## Documentation

| Document | Path |
|---|---|
| Master Specification | [`LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md`](LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md) |
| Agent Guide | [`AGENTS.md`](AGENTS.md) |
| Domain Glossary | [`docs/glossary.md`](docs/glossary.md) |
| Architecture Diagrams | [`docs/diagrams/`](docs/diagrams/) |
| Architecture Decisions | [`docs/adr/`](docs/adr/) |
| Test Strategy | [`docs/test-strategy.md`](docs/test-strategy.md) |
| Assumptions | [`docs/assumptions.md`](docs/assumptions.md) |

---

## License

[MIT](LICENSE)

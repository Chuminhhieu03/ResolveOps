# ADR-006 — .NET Aspire for Local Development Orchestration

**Date:** 2026-08-27
**Status:** Accepted
**Phase:** 1 — Runtime, Aspire, database, and observability foundation

---

## Context

Phase 1 requires a single-command local development environment that starts SQL Server, Redis, the API, and the Worker with proper dependency ordering and health checking. Two approaches were considered:

1. **.NET Aspire AppHost** — orchestrates services using .NET Aspire's distributed application model.
2. **Plain Docker Compose** — starts all containers via `docker compose up`.

---

## Decision

**Use .NET Aspire AppHost as the primary local development orchestrator.**

Aspire is used in `src/AppHost/ResolveOps.AppHost` to:
- provision SQL Server and Redis Docker containers;
- start API and Worker processes with `WaitFor` dependency ordering;
- inject connection strings automatically via Aspire resource references;
- provide the Aspire Dashboard (traces, logs, metrics, service map) at `http://localhost:18888`;
- forward OTEL signals from both hosts to the dashboard.

---

## Rationale

| Factor | Aspire | Docker Compose only |
|---|---|---|
| Connection string injection | Automatic via resource references | Manual env var management |
| Startup ordering | `WaitFor` built-in | `depends_on` with `healthcheck` |
| Local OTEL dashboard | Aspire Dashboard included | Requires Seq + Grafana setup |
| Developer experience | `dotnet run --project AppHost` | `docker compose up -d` |
| CI compatibility | Not used in CI | Used for isolated infra only |

---

## Consequences

- Docker must be running on developer machines for Aspire to start SQL Server and Redis containers.
- Aspire 9.x is used with .NET 10 application projects — Aspire 9.x supports targeting .NET 10 projects. When Aspire 10.x is released, the AppHost package will be upgraded.
- **Fallback:** `deploy/docker/docker-compose.yml` provides a plain Docker Compose alternative for machines without the .NET SDK or for demo environment infrastructure provisioning.
- AppHost is never deployed to production. The API and Worker are deployed independently.
- Aspire 9.x transitively pulls `MessagePack` and `KubernetesClient` packages with known CVEs. These have no available fixes. The AppHost project suppresses `NU1902`/`NU1903` for this reason, scoped to that project only (see `docs/assumptions.md — Phase 1`).

---

## Superseded by

None.

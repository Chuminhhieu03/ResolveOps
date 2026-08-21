# ADR-022 — Open-Source Infrastructure Strategy

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

The target delivery model is a solo developer portfolio project, designed to be production-grade. The project initially targeted Microsoft Azure, but Azure Student Credit is exhausted.

## Decision

Adopt a strict **Open-Source Infrastructure Strategy**.

- Replace all proprietary Azure services with open-source, self-hostable equivalents.
- Ensure the entire stack can run locally via Docker Compose and .NET Aspire without any cloud account.
- Deployment target will be a standard VPS (e.g., Hetzner, DigitalOcean, Oracle Cloud Always Free) using Docker Compose, rather than PaaS/Serverless cloud offerings.

**Mapping:**
- Azure SQL Database → SQL Server 2022 Developer/Express Edition container
- Azure Service Bus → RabbitMQ 3.x container
- Azure Blob Storage → MinIO container (S3-compatible)
- Azure Redis Cache → Redis 7 container
- Azure App Insights / Log Analytics → Seq container + OpenTelemetry

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| Pay for Azure | High recurring cost for a portfolio project. |
| AWS / GCP | Same cost issue. Free tiers are often too restrictive for a complete enterprise architecture. |

## Consequences

### Positive
- Zero cloud vendor lock-in.
- Zero recurring costs during development.
- Highly reproducible local environment that exactly matches production.

### Negative / Trade-offs
- Self-managing infrastructure (database backups, TLS certificates, container orchestration) adds DevOps overhead compared to fully managed PaaS.

## References

- Master Specification Section 13.8 (Explicitly rejected technologies)
- Master Specification Section 23.6 (Self-hosted production architecture)

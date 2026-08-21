# ADR-002 — SQL Server 2022 as Primary Database

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

ResolveOps requires a relational database with the following characteristics:
- ACID transactions for aggregate + outbox atomicity.
- Filtered/partial unique indexes for exception fingerprint deduplication.
- `DATETIMEOFFSET` for UTC timestamp storage with timezone awareness.
- `DECIMAL(19,4)` for monetary values.
- JSON column support (`JSON_VALUE`/`JSON_QUERY`) for policy rule definitions stored as JSON.
- Free for development and constrained production.
- EF Core first-class support.
- Docker-native image for local development and CI.

## Decision

Use **SQL Server 2022** (Developer Edition for development, Express Edition for production up to 10 GB).

ORM: **EF Core 10** with `Microsoft.EntityFrameworkCore.SqlServer` provider.

Naming convention: C# PascalCase properties mapped to `snake_case` columns via `UseSnakeCaseNamingConvention()` or explicit `HasColumnName`.

Key schema rules:
- All IDs: `UNIQUEIDENTIFIER` (Guid), generated application-side using `Guid.CreateVersion7()` (.NET 9+).
- All mutable aggregate roots: `version BIGINT` concurrency column or `ROWVERSION`.
- All tenant business tables: `tenant_id UNIQUEIDENTIFIER`.
- All timestamps: `DATETIMEOFFSET` in UTC.
- All money: `DECIMAL(19,4)`.
- All currency codes: `NCHAR(3)` (ISO 4217).
- Partial unique indexes (filtered indexes) for active exception fingerprints.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| PostgreSQL | EF Core support is excellent but the team convention is SQL Server; and SQL Server's filtered indexes, JSON operators, and `DATETIMEOFFSET` map directly to spec requirements. No material advantage for this project scope. |
| SQLite | Insufficient for production: no filtered unique indexes, limited concurrency, no `DATETIMEOFFSET`. |
| Azure SQL | Same engine, but Azure Student Credit is exhausted. Open-source Docker image achieves identical semantics at zero cost (see ADR-022). |
| MongoDB | Document store does not provide the ACID transaction guarantees required for aggregate + outbox atomicity. |

## Consequences

### Positive
- Developer and Express editions are free.
- Docker image `mcr.microsoft.com/mssql/server:2022-latest` is official and production-equivalent.
- EF Core 10 has first-class SQL Server support including `Guid.CreateVersion7()`, `DATETIMEOFFSET`, and filtered indexes.
- Testcontainers has a maintained `Testcontainers.MsSql` package.
- Filtered unique indexes enforce exception fingerprint uniqueness at the database level as a safety net.

### Negative / Trade-offs
- Express Edition has a 10 GB database size limit. Adequate for MVP/demo; production plans must monitor size.
- SQL Server is Windows-licensed for production; Developer Edition is development-only. Express Edition is free for production.
- Requires heavier Docker image than PostgreSQL/SQLite.

### Neutral
- Migrations live in `src/BuildingBlocks/ResolveOps.Persistence/`.
- Every migration must have a meaningful name and a matching rollback plan.

## References

- Master Specification Section 13.1
- Master Specification Section 15.1 (database design rules)
- SQL Server type mapping table in Section 15.1

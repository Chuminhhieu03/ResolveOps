# Assumptions

> Decisions made when requirements were ambiguous. Recorded per Section 0.2 of the master specification.
> Each assumption has a status and a link to the relevant ADR if architectural.

---

## Phase 0 Assumptions

### A-001 — Solution format: `.slnx` over `.sln`

- **Date:** 2026-08-21
- **Status:** Active
- **Context:** The master specification names the solution file `ResolveOps.slnx`. The `.slnx` format is the new XML solution format introduced in Visual Studio 17.x and supported by .NET 10 SDK. The legacy `.sln` format is not chosen.
- **Decision:** Use `.slnx`. The installed .NET 10.0.302 SDK supports it.
- **Reversible:** Yes — `dotnet sln` can convert between formats if tooling compatibility issues arise.
- **Impact:** None on business logic.

### A-002 — Aspire projects deferred to Phase 1

- **Date:** 2026-08-21
- **Status:** Active
- **Context:** Phase 0 requires creating project stubs "as justified." The Aspire AppHost and ServiceDefaults projects require the Aspire workload (`dotnet workload install aspire`). Phase 0 only requires `dotnet build` to pass. The Aspire workload installation is deferred to Phase 1 to keep Phase 0 self-contained.
- **Decision:** Create `ResolveOps.AppHost` and `ResolveOps.ServiceDefaults` as regular class library stubs in Phase 0 (they will be converted to Aspire projects in Phase 1). This satisfies the Phase 0 DoD without requiring workload installation.
- **Reversible:** Yes — Phase 1 will upgrade these projects.

### A-003 — Angular scaffold deferred to Phase 0 stub only

- **Date:** 2026-08-21
- **Status:** Active
- **Context:** Phase 0 task 9 says to "configure editor settings and .gitignore." The Angular SPA is listed in the spec but Angular CLI (`ng new`) requires Node.js and npm, which may not be available in all build environments. Phase 15 is the designated phase for the frontend.
- **Decision:** Create only `web/resolveops-web/.nvmrc` and `web/resolveops-web/package.json` stubs in Phase 0. `ng new` is run in Phase 15.
- **Reversible:** Yes — Phase 15 will scaffold the full Angular app.

### A-004 — `TreatWarningsAsErrors` with CS1591 suppressed globally

- **Date:** 2026-08-21
- **Status:** Active
- **Context:** The spec requires `TreatWarningsAsErrors=true`. CS1591 (missing XML documentation comments) would trigger on all public members across all projects with zero XML doc coverage in Phase 0–14. Adding full XML docs is valuable at a later stage but would block builds now.
- **Decision:** Suppress CS1591 globally in `Directory.Build.props`. Revisit before Phase 17 (production deployment). All other warnings remain errors.
- **Assumption note recorded in `Directory.Build.props` comment.**
- **Reversible:** Yes — remove the suppression when XML doc coverage is added.

### A-005 — No Dapper in Phase 0 packages

- **Date:** 2026-08-21
- **Status:** Active
- **Context:** Spec says "introduce Dapper after reporting queries exceed simple EF projection needs." Dapper is pre-listed in `Directory.Packages.props` for future phases but no project references it yet.
- **Decision:** Include Dapper version in central packages file but add no project references until Phase 14.

---

## Phase 1 Assumptions

### A-006 — Snake_case naming via custom convention (no external package)

- **Date:** 2026-08-27
- **Status:** Active
- **Context:** Spec §15.1 requires snake_case column and table names. `EFCore.NamingConventions` (the standard approach) transitively pulls `System.Security.Cryptography.Xml` which has unfixed high-severity CVEs in all versions. Using it would require suppressing NU1903 across all projects and shipping a known-vulnerable package.
- **Decision:** Implement snake_case via a custom `IEntityTypeAddedConvention` (table names) and an `OnModelCreating` loop (column names). No external package. The custom convention lives in `ResolveOps.Persistence/Conventions/SnakeCaseNamingConvention.cs`.
- **Reversible:** Yes — replace with `EFCore.NamingConventions.UseSnakeCaseNamingConvention()` if the transitive vulnerability is patched.
- **Impact:** None on business logic. Snake_case applies to all entities identically.

### A-007 — Single shared AppDbContext for the modular monolith

- **Date:** 2026-08-27
- **Status:** Active
- **Context:** Spec §12 describes a modular monolith with per-module boundaries. A single `AppDbContext` can share modules via `ApplyConfigurationsFromAssembly` scanning. Per-module contexts would require cross-module query complexity.
- **Decision:** Use a single `AppDbContext` in `ResolveOps.Persistence`. Module-specific entity configurations are registered via `IEntityTypeConfiguration<T>` in each module's assembly and discovered at startup. This can be refactored to per-module bounded contexts if required.
- **Reversible:** Yes — split into per-module `DbContext` if isolation requirements demand it.

### A-008 — Initial migration creates only the migrations history table

- **Date:** 2026-08-27
- **Status:** Active
- **Context:** Phase 1 requires a working `MigrateAsync()` call on integration test startup. No domain entities exist yet (they are defined from Phase 2+).
- **Decision:** The `InitialCreate` migration is an empty migration. It creates only `__EFMigrationsHistory`. Domain entity migrations are added per business phase.
- **Reversible:** Yes — subsequent migrations add domain tables.
- **Impact:** Integration test verifies `MigrateAsync()` succeeds and history table is populated.

### A-009 — Aspire AppHost suppresses NU1902/NU1903 for Kubernetes/MessagePack transitive deps

- **Date:** 2026-08-27
- **Status:** Active
- **Context:** `Aspire.Hosting.AppHost 9.3.1` transitively pulls `KubernetesClient` and `MessagePack` packages with known moderate/high severity CVEs. No fixed versions are available from those packages. The AppHost project is development/orchestration-only and is never deployed to production.
- **Decision:** Suppress `NU1902` and `NU1903` in `ResolveOps.AppHost.csproj` only. All other projects retain full vulnerability auditing.
- **Reversible:** Yes — remove suppression when Aspire releases a version with updated transitive deps.
- **ADR:** [ADR-006](../adr/ADR-006-aspire-local-orchestration.md)

### A-010 — Microsoft.EntityFrameworkCore.Design suppresses NU1903 in Persistence project

- **Date:** 2026-08-27
- **Status:** Active
- **Context:** `EFCore.Design` brings `MSBuild.Tasks.Core` which brings `System.Security.Cryptography.Xml 9.0.0` (unfixed high CVE). EFCore.Design is `PrivateAssets=all` — design-time only, never deployed.
- **Decision:** Suppress `NU1903` in `ResolveOps.Persistence.csproj` only. This is safe because the vulnerability is in MSBuild tooling, not in runtime code.
- **Reversible:** Remove suppression when a patched version of `Microsoft.Build.Tasks.Core` is released.

---

### A-013 — Tracking Ingestion Replay Guard and Out-of-Order Projection

- **Date:** 2026-09-11
- **Status:** Active
- **Context:** Carrier webhooks retry on network hiccups, and event delivery across multiple legs or carriers may arrive out of chronological order.
- **Decision:** Inbound receipts enforce a filtered unique index `(tenant_id, source_system, external_event_id)` to silently ignore replays with HTTP 202 without publishing duplicate normalization work. The `Shipment` aggregate milestone projection retains terminal status (`Delivered`) and earliest pickup timestamps when late out-of-order events arrive.
- **Reversible:** Yes — milestone transition logic can be adjusted in `Shipment.ApplyTrackingEvent`.
- **Impact:** Architectural / Invariant.
- **ADR:** [ADR-023](adr/ADR-023-tracking-ingestion-and-quarantine.md).

---

```markdown
### A-NNN — Title

- **Date:** YYYY-MM-DD
- **Status:** Active | Superseded | Resolved
- **Context:** Why the ambiguity exists.
- **Decision:** What was decided.
- **Reversible:** Yes/No — how to change it.
- **Impact:** Business / Architectural / None.
- **ADR:** [ADR-NNN](../adr/ADR-NNN-title.md) if architectural.
```

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

## Template for new assumptions

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

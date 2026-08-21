# Changelog

All notable changes to ResolveOps are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- Phase 1 will add runtime, Aspire, database, and observability foundation.

---

## [0.0.0] — 2026-08-21

### Phase 0 — Product and Repository Foundation

#### Added
- Repository structure matching specification Section 14.
- `global.json` pinning .NET SDK 10.0.302.
- `Directory.Build.props` with nullable reference types, warnings-as-errors,
  latest analyzer rules, and deterministic builds.
- `Directory.Packages.props` with centralized NuGet package version management.
- `.editorconfig` enforcing C# code style (file-scoped namespaces, underscore private fields, LF).
- `.gitignore` for .NET 10, Angular, Docker, and IDE artifacts.
- `.gitattributes` for consistent LF line endings.
- MIT License.
- `AGENTS.md` — coding agent operating guide with build commands, constraints, naming rules.
- `README.md` — product overview, non-goals, architecture summary.
- `CHANGELOG.md` — this file.
- Empty compilable solution `ResolveOps.slnx` with all host, building-block, module, and test project stubs.
- `docs/glossary.md` — domain glossary from spec Section 5.
- `docs/assumptions.md` — initial Phase 0 assumptions.
- `docs/test-strategy.md` — testing pyramid and coverage policy.
- `docs/adr/ADR-TEMPLATE.md` — standard ADR format.
- `docs/adr/ADR-001-modular-monolith.md`
- `docs/adr/ADR-002-sql-server-2022.md`
- `docs/adr/ADR-003-vertical-slices-no-mediator.md`
- `docs/adr/ADR-004-rabbitmq-broker.md`
- `docs/adr/ADR-005-minio-object-storage.md`
- `docs/adr/ADR-021-angular-frontend.md`
- `docs/adr/ADR-022-open-source-infrastructure.md`
- Architecture diagrams (Mermaid):
  - `docs/diagrams/context-diagram.md`
  - `docs/diagrams/container-diagram.md`
  - `docs/diagrams/module-diagram.md`
  - `docs/diagrams/tracking-ingestion-sequence.md`
  - `docs/diagrams/damage-claim-sequence.md`
- Stub files for `web/resolveops-web/` (`.nvmrc`, `package.json`).
- Placeholder directories: `deploy/`, `tools/`.

# Changelog

All notable changes to ResolveOps are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- **Phase 9 (Evidence and secure document pipeline)**:
  - Added `EvidenceDocument` aggregate and `EvidenceRequirement` domain entities in `ResolveOps.Domain.Documents`.
  - Added `EvidenceType` (`DeliveryReceiptProof`, `PhotosOfDamageProof`, `CommercialInvoiceProof`, `PackingListProof`, `CarrierInspectionReport`, `WeightCertificate`, `CustomsDocumentation`, `CustomerAffidavit`, `WrittenDenialOrCorrespondence`, `PoliceReport`, `Other`), `DocumentStatus` (`PendingUpload`, `PendingScan`, `Available`, `RejectedScanFailed`, `Superseded`, `Archived`), and `DocumentScanStatus` (`NotScanned`, `Clean`, `Infected`, `ScanFailed`, `Exempt`).
  - Added S3-compatible `IObjectStorageService` / `MinIoObjectStorageService` implementation using `AWSSDK.S3` for generating presigned upload/download URLs with strictly bounded TTLs and non-guessable storage object keys (`tenants/{tenantId}/{evidenceType}/{yyyy}/{MM}/{guid}-{sanitizedFileName}`).
  - Implemented `IMalwareScanner` and `DevelopmentMalwareScanner` with deterministic detection of standard EICAR test signatures.
  - Implemented `DocumentProcessingWorker` background service in `ResolveOps.Worker`: scans pending uploads, verifies file size and SHA-256 hash against storage stream, scans for malware signatures, transitions status to `Available` or `RejectedScanFailed`, and atomically emits `EvidenceAvailableV1` to the Outbox.
  - Implemented `AbandonedUploadCleanupJob` Quartz.NET job running hourly to clean up documents stuck in `PendingUpload` past TTL.
  - Implemented 8 vertical slice Minimal API endpoints in `ResolveOps.Modules.Documents`:
    - `POST /api/evidence/upload-intent` (creates upload intent & presigned PUT URL)
    - `POST /api/evidence/{id}/complete-upload` (notifies upload completion, schedules verification)
    - `POST /api/evidence/{id}/download-intent` (generates short-lived presigned GET URL with audit trail)
    - `GET /api/evidence/{id}` (retrieves document metadata)
    - `GET /api/exceptions/{caseId}/evidence` (lists evidence documents linked to a case)
    - `POST /api/evidence/{id}/supersede` (supersedes an existing document with a new revision, preserving version history)
    - `DELETE /api/evidence/{id}` (soft-deletes/archives document)
    - `GET /api/claims/{claimId}/evidence-checklist` (evaluates uploaded evidence against carrier requirement definitions)
  - Added EF Core configurations for `EvidenceDocument` and `EvidenceRequirement` with tenant query filters, foreign keys, and indexes.
  - Generated EF Core migration `20260919140212_AddEvidenceAndSecureDocumentPipeline`.
  - Added OpenTelemetry metrics (`documents.uploaded.total`, `documents.scanned.total`, `documents.scan_failed.total`, `documents.downloaded.total`) and meter `ResolveOps.Documents` in `ResolveOps.Observability`.
  - Added ADR-026 (`docs/adr/ADR-026-evidence-and-secure-document-pipeline.md`).

- **Phase 8 (Exception case workflow, tasks, SLA)**:
  - Added `WorkflowTask`, `SlaPolicy`, `SlaPolicyVersion`, `SlaClock`, and `SlaClockPause` domain entities in `ResolveOps.Domain.Workflow`.
  - Added `WorkflowTaskStatus`, `WorkflowTaskPriority`, `WorkflowTaskType`, `SlaClockStatus`, `SlaClockType` domain enums.
  - Extended `ExceptionCase` aggregate root with guarded domain methods strictly enforcing the transition matrix (§9.1): `Triage`, `Assign`, `StartInvestigation`, `RequestEvidence`, `ReceiveEvidence`, `RecordCarrierUpdate`, `StartMitigation`, `CompleteMitigation`, `MarkClaimRequired`, `Resolve`, `Close`, `Reopen`, `ChangeSeverity`, `Reclassify`, and `AddComment`.
  - Enforced Master Spec Invariant §10.3: Cannot close a case with incomplete mandatory tasks unless explicitly waived with reason and actor attribution.
  - Implemented optimistic concurrency via `ConcurrencyStamp` (ADR-006) across all case lifecycle transitions and task mutations (returning HTTP 409 Conflict).
  - Implemented `IBusinessCalendarService` / `BusinessCalendarService` for calculating deadlines using working hours, operating schedules, partner calendars, and holiday schedules (§16.7).
  - Implemented `ISlaClockService` / `SlaClockService` managing response and resolution SLA clocks, clock pausing on external waiting states, and pause-adjusted resumption (§9.4).
  - Implemented Quartz.NET `SlaBreachScanJob` running every minute in `ResolveOps.Worker`, scanning for running clocks exceeding target deadlines, marking breaches, and atomically publishing `CaseSlaBreachedV1` to the Outbox (§18.1).
  - Implemented Workflow Tasks minimal API endpoints in `ResolveOps.Modules.Workflow`: `GET /api/tasks/my`, `GET /api/exception-cases/{id}/tasks`, `POST /api/exception-cases/{id}/tasks`, `POST /api/tasks/{id}/assign`, `POST /api/tasks/{id}/start`, `POST /api/tasks/{id}/block`, `POST /api/tasks/{id}/unblock`, `POST /api/tasks/{id}/complete`, `POST /api/tasks/{id}/cancel`, `POST /api/tasks/{id}/waive`.
  - Implemented Exception Case lifecycle API endpoints in `ResolveOps.Modules.Exceptions`: `POST /api/exception-cases/{id}/triage`, `assign`, `start-investigation`, `change-severity`, `reclassify`, `request-evidence`, `receive-evidence`, `record-carrier-update`, `start-mitigation`, `complete-mitigation`, `mark-claim-required`, `resolve`, `close`, `reopen`, `comments`, and `GET /api/exception-cases/{id}/timeline`.
  - Implemented OpenTelemetry metrics (`sla.breaches.total`, `sla.evaluations.total`, `tasks.created.total`, `tasks.completed.total`) and meter `ResolveOps.Workflow` in `ResolveOps.Observability`.
  - Added EF Core configurations and migration `20260917152415_AddWorkflowTasksAndSlaClocks`.
  - Added ADR-025 (`docs/adr/ADR-025-exception-case-workflow-and-sla.md`).

- **Phase 7 (Exception policy engine and case creation)**:
  - Added `ExceptionPolicy`, `ExceptionCase`, `ExceptionOccurrence`, and `CaseTimelineEntry` domain entities in `ResolveOps.Domain.Exceptions`.
  - Implemented `ExceptionType` (`PickupDelay`, `InTransitDelay`, `Damage`), `ExceptionCaseStatus`, `ExceptionSeverity`, `PolicyStatus`, `CaseTimelineEntryType`, and `ActorType` domain models.
  - Implemented `ExceptionPolicyConfiguration`, `ExceptionCaseConfiguration`, `ExceptionOccurrenceConfiguration`, and `CaseTimelineEntryConfiguration` in `ResolveOps.Persistence.Configurations`.
  - Enforced filtered unique partial index `UIX_ExceptionCases_ActiveFingerprint (tenant_id, fingerprint) WHERE status NOT IN ('Closed', 'Cancelled')` on `exception_cases` to prevent duplicate active cases across repeated scans and events.
  - Added tenant query filters for all new Exception entities in `AppDbContext`.
  - Generated EF Core migration `20260917142552_AddExceptionPoliciesAndCases`.
  - Implemented `ExceptionFingerprintGenerator` producing deterministic composite fingerprints (`TenantId:ShipmentId:LegId:Type:BusinessKey:vVersion`).
  - Implemented `SeverityCalculator` (Low, Medium, High, Critical) and `AssignmentEvaluator` based on versioned policy JSON schemas.
  - Implemented `ExceptionPolicyEvaluator` for deterministic detection of `PickupDelay`, `InTransitDelay`, and `Damage`.
  - Implemented `ExceptionDetectedV1` integration event in `ResolveOps.Messaging.Events` and outbox publishing.
  - Implemented `ExceptionEvaluationConsumerService` in `ResolveOps.Worker`: transactional Inbox idempotency consuming `TrackingEventAcceptedV1`, evaluating incoming signals, appending occurrences or creating cases, with concurrency race handling.
  - Implemented `MissedDeadlineScanJob` using Quartz.NET: scans active shipments for missed planned pickup times and milestone deadlines against configured tolerances.
  - Implemented policy management API endpoints: `POST /api/exception-policies`, `GET /api/exception-policies`, `GET /api/exception-policies/active`, `POST /api/exception-policies/{id}/activate`, `POST /api/exception-policies/{id}/retire`.
  - Implemented case endpoints: `GET /api/exception-cases` (paged/filtered), `GET /api/exception-cases/{id}` (with occurrences & timeline), `POST /api/exception-cases` (manual creation), `POST /api/exception-cases/{id}/cancel` (false-positive cancellation with reason).
  - Added `CanCreateExceptionCase` and `RequireCreateExceptionCase` / `RequireCancelCase` security policies.
  - Implemented OpenTelemetry metrics (`exceptions.detected.total`, `exceptions.detection.duration.ms`) and `ActivitySource` in `ResolveOps.Observability`.
  - Added ADR-024 (`docs/adr/ADR-024-exception-policy-engine-and-fingerprinting.md`).

- **Phase 6 (Tracking ingestion and normalization)**:
  - Added `InboundEventReceipt`, `TrackingEvent`, and `QuarantinedEvent` domain entities in `ResolveOps.Domain.Tracking`.
  - Implemented `TrackingEventType`, `InboundReceiptStatus`, `QuarantinedEventStatus`, and `QuarantineReasonCodes` constants.
  - Implemented `DemoCarrierAdapter` with constant-time HMAC-SHA256 signature validation (`X-Signature`), parsing, and normalization.
  - Implemented `DemoCarrierFixtures` with documented test payloads for Pickup, InTransit, OutForDelivery, Delivered, Damage, and Unmatched events.
  - Implemented `POST /api/integrations/carriers/{carrierCode}/webhooks/tracking` with replay guard on `(tenant_id, source_system, external_event_id)` and fast `202 Accepted` response.
  - Implemented `POST /api/tracking-events` for manual tracking event ingestion.
  - Implemented Quarantine management endpoints: `GET /api/integration-operations/quarantined-events`, `GET .../{id}`, `POST .../{id}/resolve`, and `POST .../{id}/reprocess`.
  - Implemented `TrackingIngestionConsumerService` in `ResolveOps.Worker`: consumes from RabbitMQ `resolveops.tracking-ingestion`, enforces transactional Inbox idempotency, matches shipment via `ShipmentTrackingAlias` or `ExternalReference`, creates canonical `TrackingEvent`, safely projects actual milestones on `Shipment` aggregate without regression on out-of-order events, and writes `TrackingEventAcceptedV1` to outbox.
  - Implemented OpenTelemetry metrics (`tracking.receipts.total`, `tracking.normalized.total`, `tracking.quarantined.total`, `tracking.normalization.duration.ms`) and `ActivitySource` in `ResolveOps.Observability`.
  - Added `AddTrackingAndQuarantine` EF Core migration.
  - Added ADR-023 (`docs/adr/ADR-023-tracking-ingestion-and-quarantine.md`) and assumption A-013.

- **Phase 4 (Shipment domain)**:
  - Added `Shipment` aggregate root with `ShipmentLeg`, `ShipmentItem`, and `ShipmentTrackingAlias` child entities.
  - Implemented `ShipmentStatus`, `ShipmentLegStatus`, `TrackingAliasType` constants.
  - Implemented `CreateShipment` with API idempotency key support (`Idempotency-Key` header).
  - Implemented `GetShipment`, `ListShipments` (offset pagination, status filter), and `CancelShipment`.
  - Optimistic concurrency via `ConcurrencyStamp` on cancel.
  - Duplicate external reference guard with `UniqueConstraintException` race condition handling.
  - `ShipmentCreatedV1` integration event written atomically with aggregate via outbox.
  - EF Core configurations for `shipments`, `shipment_legs`, `shipment_items`, `shipment_tracking_aliases`.
  - Tenant query filters for all Shipment entities.
  - Added `RequireCancelShipment` permission/policy to `ResolveOps.Security`.
  - Registered `ShipmentsModule` in `ResolveOps.Api`.

- **Phase 5 (Messaging foundation)**:
  - Added `OutboxMessage`, `InboxMessage`, `IdempotencyRecord` domain entities.
  - Implemented `IOutboxWriter` / `OutboxWriter` — writes outbox rows atomically in handler `SaveChanges`.
  - Implemented `OutboxPublisherService` (`BackgroundService` in `ResolveOps.Worker`) — polls `outbox_messages`, publishes via RabbitMQ, exponential back-off up to 5 attempts.
  - Implemented `RabbitMqPublisher` using `RabbitMQ.Client v7` async API; declares durable fanout exchange per event type.
  - Implemented `IntegrationEventEnvelope` standard message envelope.
  - Implemented `ShipmentCreatedV1` integration event.
  - EF Core configurations for `outbox_messages` (with required publisher index), `inbox_messages`, `idempotency_records`.
  - Registered `RabbitMqPublisher` and `OutboxPublisherService` in `ResolveOps.Worker`.
  - Added `AddShipmentsAndMessaging` EF Core migration.

## [2026-09-03]
- **Phase 2: Tenancy and Identity**
  - Added ASP.NET Core Identity with EF Core integration (`ApplicationUser`, roles, claims).
  - Implemented JWT token generation and authentication.
  - Added robust refresh token rotation (`RefreshTokenSession`) with family support for improved security.
  - Added `Tenant`, `TenantSettings`, and `UserTenantMembership` entities to support multi-tenancy.
  - Enforced tenant isolation via EF Core query filters.
  - Implemented initial local development seeder (`DevelopmentSeeder`) for `local-dev` tenant and admin user.
  - Wired Tenancy and Identity modules into `AppDbContext` and API endpoints.

---

## [0.1.0] — 2026-08-27

### Phase 1 — Runtime, Aspire, Database, and Observability Foundation

#### Added
- `ResolveOps.ServiceDefaults` — shared Serilog, OpenTelemetry traces/metrics, and health-check extensions used by API and Worker.
- `ResolveOps.Persistence` — `AppDbContext` with custom snake_case naming convention, `ApplyConfigurationsFromAssembly`, and `SaveChangesAsync` outbox hook point. `AppDbContextDesignTimeFactory` for `dotnet ef` CLI.
- `Migrations/InitialCreate` — empty initial EF Core migration (creates `__EFMigrationsHistory` only).
- `ResolveOps.AppHost` — .NET Aspire AppHost orchestrating SQL Server, Redis, API, and Worker with `WaitFor` ordering.
- `ResolveOps.Api` — real ASP.NET Core Minimal API host with Aspire EF Core + Redis integration, Problem Details, OpenAPI, `CorrelationMiddleware` (`X-Correlation-ID`), `/health/live`, `/health/ready`, and `/api/version`.
- `ResolveOps.Worker` — real Worker host with health endpoints via `WebApplication` builder.
- `appsettings.json` / `appsettings.Development.json` for API and Worker — structured config, no secrets committed.
- `ResolveOps.ArchitectureTests` — NetArchTest.Rules tests enforcing Domain/Application/Persistence dependency boundaries and AutoMapper prohibition.
- `ResolveOps.IntegrationTests` — Testcontainers SQL Server migration tests (`MigrateAsync` success, idempotency, history table existence).
- `.github/workflows/ci.yml` — GitHub Actions CI: restore → format check → Release build → architecture tests → integration tests.
- `deploy/docker/docker-compose.yml` — plain Docker Compose fallback for SQL Server, Redis, RabbitMQ, MinIO, Seq, Mailpit.
- `deploy/docker/.env.example` — environment variable template (no secrets committed).
- `docs/adr/ADR-006-aspire-local-orchestration.md` — decision record for Aspire as local orchestrator.
- Assumptions A-006 through A-010 recorded in `docs/assumptions.md`.
- Assembly markers (`AssemblyMarker.cs`) in `ResolveOps.Domain` and `ResolveOps.Application`.

#### Changed
- `Directory.Packages.props` — added `Aspire.Hosting.SqlServer`, `Aspire.Hosting.Redis`; upgraded OpenTelemetry packages from `1.12.0` to `1.18.0`; upgraded `OpenTelemetry.Instrumentation.SqlClient` from `0.1.0-beta.4` to `1.18.0`; upgraded `Microsoft.AspNetCore.OpenApi` from `10.0.0` to `10.0.11`.

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

# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 9.

---

## PROMPT (copy từ đây)

---

You are implementing **ResolveOps**, a Logistics Exception & Carrier Claims management platform.

## Specification

The root specification is:
```
c:\Personal\ResolveOps\LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md
```
*(Hoặc `c:\Personal\ResolveOps\LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md` tùy môi trường máy).*

Read the specification before making any changes. Pay special attention to:
- Section 0: Agent rules and non-negotiable constraints
- Section 8.6: Evidence collection workflow & metadata requirements
- Section 9.1: Exception case states & evidence integration (`AwaitingEvidence` -> `Investigating`)
- Section 10.4: Evidence invariants (scanned before available, single tenant, non-guessable partition path, MIME type validation, duplicate checksum check, versioning/superseding, legal hold, no permanent public URLs)
- Section 12: Architecture and module structure (`ResolveOps.Modules.Documents`, `ResolveOps.Domain`, `ResolveOps.Application`)
- Section 13.3: Technology stack (MinIO / AWSSDK.S3)
- Section 15.9: Document tables (`evidence_documents`, `evidence_requirements`)
- Section 16.9: Evidence endpoints (`/api/exceptions/{caseId}/evidence`, upload intents, complete-upload, download intent, supersede, remove, evidence checklist)
- Section 17.3: Outbox integration event (`EvidenceAvailableV1`)
- Section 18.1 & 18.2: Required workers & scheduler (`DocumentProcessingWorker`, `AbandonedUploadCleanupJob` in Quartz.NET)
- Section 24: Implementation roadmap and Phase 9 definition
- Section 25: Coding standards
- `AGENTS.md` in the repository root and `docs/adr/ADR-005-minio-object-storage.md`

## Technology Stack (mandatory — do not substitute)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 LTS, C# |
| Web API | ASP.NET Core Minimal APIs |
| ORM | EF Core 10 with SQL Server provider |
| Database | SQL Server 2022 (Docker: `mcr.microsoft.com/mssql/server:2022-latest`) |
| Message Broker | RabbitMQ 3.x (Docker: `rabbitmq:3-management`) — client: `RabbitMQ.Client v7` |
| Scheduler | Quartz.NET (in `ResolveOps.Worker`) |
| Business Calendar | `IBusinessCalendarService` (tenant-scoped working hours & holiday calculation) |
| Object Storage | MinIO (Docker: `minio/minio`) — client: `AWSSDK.S3` (S3-compatible presigned URLs) |
| Cache | Redis 7 (Docker: `redis:7-alpine`) — client: `StackExchange.Redis` |
| Observability | OpenTelemetry + Serilog + Seq (`datalust/seq`) |
| Local Orchestration | .NET Aspire + Docker Compose |
| Frontend | Angular 19+ + TypeScript + Angular Material (Phase 15+) |
| Testing | Architecture tests (`ResolveOps.ArchitectureTests`) |
| Concurrency | Optimistic Concurrency via `string ConcurrencyStamp` (ADR-006 & centralized `AppDbContext`) |

**Rejected (do not add):** Azure proprietary services, MassTransit, AutoMapper, generic repositories, React/Vite, microservices, Kubernetes, Kafka.

## Current Implementation State

**Current phase:** `Phase 9 — Evidence and secure document pipeline`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings).
- Architecture tests pass (5 passed, 0 failed).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (AddHandlersFromAssembly, MapEndpoints).
- DomainErrors unified via ErrorTemplates table and DatabaseErrorMessageProvider returning RFC 7807 Problem Details.
- Centralized optimistic concurrency via string ConcurrencyStamp in AppDbContext.ApplyAuditAndConcurrency() (domain methods do not manually assign stamps).
- Database schema: Migrations through 20260917152415_AddWorkflowTasksAndSlaClocks are applied.
- Background workers running in ResolveOps.Worker:
  - TrackingIngestionConsumerService (RabbitMQ v7 async consumer for raw carrier receipts)
  - ExceptionEvaluationConsumerService (RabbitMQ v7 async consumer for TrackingEventAcceptedV1)
  - MissedDeadlineScanJob (Quartz.NET 1-min periodic scan for missed shipment milestones)
  - SlaBreachScanJob (Quartz.NET 1-min periodic scan for breached SLA clocks emitting CaseSlaBreachedV1)
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL test requirements are currently waived. Architecture tests remain strictly required.
```

**Stopping point / specific task this session:**
```
Implement Phase 9: Evidence and secure document pipeline according to Master Spec §24 Phase 9, §8.6, §9.1, §10.4, §12, §13.3, §15.9, §16.9, §17.3, §18.1–18.2:

1. Evidence Domain Aggregate & Requirements (Spec §8.6, §10.4, §15.9):
   - Create domain entities in `src/BuildingBlocks/ResolveOps.Domain/Documents/`:
     - `EvidenceDocument`:
       - Fields: `Id`, `TenantId`, `CaseId`, `ClaimId` (nullable), `EvidenceType` (SignedPOD, DamagePhotos, CommercialInvoice, PackingList, InspectionReport, CarrierNotice, RepairEstimate, SalvageReceipt, WeightCertificate, Other), `Status` (PendingUpload, PendingScan, Available, Superseded, Rejected, Quarantined, Removed), `OriginalFileName`, `StorageObjectName`, `StorageContainer`, `ContentType`, `SizeBytes`, `Sha256`, `DocumentDate`, `Issuer`, `VersionNumber`, `SupersedesDocumentId` (nullable), `UploadedBy`, `UploadedAtUtc`, `ScanStatus` (Pending, Clean, Malicious, Failed), `ScanCompletedAtUtc`, `RetentionUntil`, `LegalHold`, `ConcurrencyStamp`.
       - Domain methods: `CompleteUpload`, `MarkScanClean`, `MarkScanMalicious`, `MarkScanFailed`, `Supersede`, `MarkRemoved`, `ToggleLegalHold`.
       - Invariant §10.4: Evidence unavailable to business workflows until scan status is `Available`.
       - Invariant §10.4: Cannot remove a document under `LegalHold`.
       - Invariant §10.4: Superseding does not erase prior versions; older document enters status `Superseded`.
     - `EvidenceRequirement`:
       - Fields: `Id`, `TenantId`, `PolicyVersionId`, `ExceptionType`, `ClaimType` (nullable), `EvidenceType`, `IsMandatory`, `ConditionJson`.
   - Implement `EvidenceType`, `DocumentStatus`, `ScanStatus` constants/enums.

2. Object Storage Service & MinIO / S3 Integration (Spec §13.3, ADR-005):
   - Add `AWSSDK.S3` dependency to Central Package Management (`Directory.Packages.props`) and project references.
   - Define `IObjectStorageService` in `ResolveOps.Application`:
     - `GenerateUploadPresignedUrlAsync(string container, string objectName, string contentType, TimeSpan expiry, CancellationToken ct)`
     - `GenerateDownloadPresignedUrlAsync(string container, string objectName, TimeSpan expiry, string? downloadFileName, CancellationToken ct)`
     - `ObjectExistsAsync(string container, string objectName, CancellationToken ct)`
     - `GetObjectMetadataAsync(string container, string objectName, CancellationToken ct)`
     - `OpenReadStreamAsync(string container, string objectName, CancellationToken ct)`
     - `DeleteObjectAsync(string container, string objectName, CancellationToken ct)`
   - Implement `MinIoObjectStorageService` using `IAmazonS3` configured with MinIO endpoint, credentials, and path-style addressing (`ForcePathStyle = true`).
   - Tenant isolation: Object path MUST use tenant partition: `tenants/{tenantId}/cases/{caseId}/{evidenceType}/{documentId}_{safeFileName}`.
   - Presigned URLs: Short-lived expiration (e.g. 15-30 minutes); permanent public URLs must NEVER be exposed.

3. Malware Scanner Abstraction & Processing Pipeline (Spec §8.6, §10.4, §18.1):
   - Define `IMalwareScanner` in `ResolveOps.Application`:
     - `ScanAsync(Stream contentStream, string fileName, CancellationToken ct)` returning `MalwareScanResult` (IsClean, ThreatName, ScanEngine).
   - Implement a safe mock/stub scanner (`SafeMalwareScanner` / `DevelopmentMalwareScanner`) that inspects content streams, detects mock infection test markers (e.g. EICAR or configurable test signatures), and safely marks real files as clean.
   - Implement `DocumentProcessingWorker` in `ResolveOps.Worker`:
     - Consumes document upload completion signals (or processes `PendingScan` documents).
     - Validates uploaded object existence in MinIO and verifies actual size.
     - Computes SHA-256 hash streamingly; checks for duplicate checksums within the case/tenant.
     - Invokes `IMalwareScanner`.
     - If clean: transitions document to `ScanStatus.Clean` and `Status.Available`, writes `EvidenceAvailableV1` to Outbox, and triggers case timeline update.
     - If infected: transitions document to `ScanStatus.Malicious` and `Status.Quarantined`, logs security alert.
   - Implement `AbandonedUploadCleanupJob` in `ResolveOps.Worker` (Quartz.NET periodic job):
     - Scans `evidence_documents` where `status = 'PendingUpload'` and `uploaded_at_utc` exceeded expiration window (e.g. 24h), marking them as abandoned/removed.

4. Evidence REST APIs (`ResolveOps.Modules.Documents`) (Spec §16.9):
   - `GET /api/exceptions/{caseId}/evidence`: List all evidence documents for case.
   - `POST /api/exceptions/{caseId}/evidence/upload-intents`:
     - Request: `evidenceType`, `fileName`, `contentType`, `sizeBytes`, `documentDate`, `issuer`.
     - Validates allowed MIME types and max file size (e.g., 25MB for images/PDFs).
     - Creates `EvidenceDocument` in `PendingUpload` status with non-guessable storage object path.
     - Returns: `documentId`, `uploadMethod = "SignedUrl"`, `uploadUrl`, `expiresAt`.
   - `POST /api/evidence/{documentId}/complete-upload`:
     - Request: `sha256` (client hash).
     - Transitions status to `PendingScan` and enqueues for background verification.
   - `GET /api/evidence/{documentId}`: Returns metadata, scan status, version info.
   - `POST /api/evidence/{documentId}/download-intent`:
     - Verifies scan status == `Available`.
     - Returns short-lived presigned GET URL with custom content-disposition filename.
   - `POST /api/evidence/{documentId}/supersede`:
     - Initiates new version upload intended to supersede an existing document.
     - Links `SupersedesDocumentId` and increments `VersionNumber`.
   - `POST /api/evidence/{documentId}/remove`:
     - Checks `LegalHold == false`. Transitions status to `Removed`.
   - `GET /api/claims/{claimId}/evidence-checklist`:
     - Evaluates mandatory and optional `EvidenceRequirements` against active `Available` documents.

5. Persistence & Integration Events:
   - EF Core configurations in `ResolveOps.Persistence`:
     - `EvidenceDocumentConfiguration`
     - `EvidenceRequirementConfiguration`
   - Tenant query filters in `AppDbContext` for `EvidenceDocument` and `EvidenceRequirement`.
   - Composite indexes: `evidence_documents(tenant_id, case_id, evidence_type, status)` and `evidence_documents(tenant_id, sha256)`.
   - EF Core Migration: `AddEvidenceAndSecureDocumentPipeline`.
   - Integration Event in `ResolveOps.Messaging`: `EvidenceAvailableV1(Guid DocumentId, Guid CaseId, Guid? ClaimId, string EvidenceType, DateTimeOffset AvailableAtUtc)`.

6. Observability & Documentation:
   - OpenTelemetry metrics in `DocumentMetrics.cs` (`documents.uploaded.total`, `documents.scanned.total`, `documents.quarantined.total`, `documents.scan.duration.ms`).
   - Create `docs/adr/ADR-026-evidence-and-secure-document-pipeline.md`.
   - Update `AGENTS.md` and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user).
Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 9 from the specification.
3. **Implement only the Phase 9 scope** — do not add features from future phases (e.g., Claim aggregate in Phase 10, Carrier responses in Phase 11).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Documents`, `ResolveOps.Domain`, `ResolveOps.Persistence`).
5. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
6. **Ensure ConcurrencyStamp integrity**: Concurrency stamps are managed centrally by `AppDbContext.ApplyAuditAndConcurrency()`; domain methods MUST NOT manually mutate stamps.
7. **Add/update** EF Core migrations, OpenTelemetry instrumentation, and ADR-026.
8. **Run** formatting (`dotnet format`), build (`dotnet build ResolveOps.slnx -c Release`), and architecture tests (`dotnet test tests/ResolveOps.ArchitectureTests/ -c Release`).
9. **Fix** any failures caused by your changes before reporting done.
10. **Update** `CHANGELOG.md` and `AGENTS.md` with current phase status.
11. **Report** at the end: files changed, commands run, test results, assumptions made, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT expose permanent direct public URLs to storage objects — always use short-lived presigned URLs or authorized streaming.
- Do NOT trust file extensions as content types.
- Do NOT allow unscanned or quarantined files to satisfy evidence requirements.
- Do NOT delete files under `LegalHold`.
- Do NOT use AutoMapper — mapping must be explicit.
- Do NOT put business logic in endpoints.
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user).

## Definition of Done Checklist (Section 31 & §24 Phase 9)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] `EvidenceDocument` and `EvidenceRequirement` entities implemented with guarded methods
- [ ] MinIO / S3 object storage service generates short-lived presigned upload/download URLs
- [ ] Storage paths are non-guessable and partitioned by tenant (`tenants/{tenantId}/...`)
- [ ] Permanent public storage URLs are never exposed
- [ ] Unscanned or quarantined files cannot satisfy evidence checklists
- [ ] Pluggable malware scanner inspects files and quarantines malicious content
- [ ] Document processing worker verifies SHA-256, validates size/existence, and emits `EvidenceAvailableV1` to Outbox
- [ ] Document versioning/superseding preserves prior versions for audit history
- [ ] Documents under legal hold cannot be removed
- [ ] Quartz.NET job cleans up abandoned upload intents
- [ ] Cross-tenant document access is prevented by tenant query filters and authorization
- [ ] EF Core migration applied for evidence documents and requirements
- [ ] OpenTelemetry metrics and activity sources instrumented for document pipeline
- [ ] ADR-026 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Ví dụ |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 9 — Evidence and secure document pipeline` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0, 1, 2, 3, 4, 5, 6, 7, 8` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Số migration, số test, file nào đang có |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai MinIO storage, evidence aggregate, document worker |

### Cách lấy repository state nhanh:

Chạy lệnh này trong repo để lấy thông tin điền vào:
```powershell
# Chạy trong thư mục repo
Write-Host "=== BUILD ===" ; dotnet build --no-restore -q 2>&1 | tail -3
Write-Host "=== ARCH TESTS ===" ; dotnet test tests/ResolveOps.ArchitectureTests/ --no-build -q 2>&1 | tail -5  
Write-Host "=== MIGRATIONS ===" ; dotnet ef migrations list --project src/BuildingBlocks/ResolveOps.Persistence 2>&1
Write-Host "=== GIT STATUS ===" ; git log --oneline -5
```

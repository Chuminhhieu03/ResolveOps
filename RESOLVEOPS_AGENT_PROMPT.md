# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 14.

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
- Section 0: Agent rules and non-negotiable constraints (Rule 4: AI is strictly forbidden from financial state transitions; Rule 10: no database transactions around slow external HTTP or exports; Rule 13: treat inbound webhooks/queue messages as untrusted; Rule 14: tenant isolation; Rule 15: cancellation tokens; Rule 16: structured logging without PII or credentials)
- Section 4.1 & 4.2: Recipient personas and roles (Operations Manager, Claims Specialist, Logistics Coordinator, Finance, Carrier External)
- Section 10.6: Tenant invariants (tenant isolation on all queries, reports, exports, read models; use `AppDbContext` global query filters without redundant `.Where(x => x.TenantId == tenantId)`)
- Section 11 & Section 19.9: Edge cases & Security:
  - CSV formula injection protection: any text beginning with `=`, `+`, `-`, `@`, `\t`, `\r` must be safely escaped (e.g. prepended with `'`) so spreadsheet applications (Excel, Calc) cannot execute arbitrary formulas or macro injection (DDE/CMD).
  - Unauthorized export download / IDOR protection: export requests must be strictly tenant-isolated and only accessible to authorized users.
  - Large asynchronous export handling: exports run asynchronously via background jobs, stream results to MinIO object storage in container `exports`, and expose temporary presigned download URLs.
- Section 12.2 & 12.3: Architecture and module structure (`ResolveOps.Modules.Reporting`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`, `ResolveOps.Security`, `ResolveOps.Worker`)
- Section 12.4: Command/query model: Dapper (pinned at 2.1.66) and/or EF Core projections for measured complex analytical queries; Vertical Slice Architecture (1 class per file).
- Section 15: Database schema & reporting tables (`export_requests`, `carrier_performance_snapshots` / read models)
- Section 16.12: Reporting endpoints:
  - `GET /api/dashboard/operations`
  - `GET /api/dashboard/claims`
  - `GET /api/reports/exception-ageing`
  - `GET /api/reports/sla-performance`
  - `GET /api/reports/carrier-scorecards`
  - `GET /api/reports/financial-recovery`
  - `POST /api/exports/exception-cases`
  - `GET /api/exports/{exportId}`
- Section 17.3 & 17.4: Core integration events consumed by Reporting read models:
  - `ShipmentCreatedV1`, `TrackingEventAcceptedV1`, `ExceptionDetectedV1`, `CaseSlaBreachedV1`, `ClaimSubmittedV1`, `ClaimDecisionRecordedV1`, `ClaimRecoveryRecordedV1`, `EvidenceAvailableV1`
- Section 18.1: Background workers (`ReportingProjectionConsumerService` in `ResolveOps.Worker` for async read model projections, `ExportProcessingJob` via Quartz.NET for async CSV exports)
- Section 19.9 & 22.7: Security (CSV formula injection escaping, IDOR prevention on export downloads, tenant-scoped storage paths)
- Section 24: Implementation roadmap and Phase 14 definition
- Section 25: Coding standards (C# 13 / .NET 10, explicit mapping, no generic repo, 1 class per file)
- `AGENTS.md` in the repository root and `docs/adr/ADR-006-auto-discovery-and-concurrency.md`

## Technology Stack (mandatory — do not substitute)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 LTS, C# |
| Web API | ASP.NET Core Minimal APIs |
| ORM | EF Core 10 with SQL Server provider |
| Query Engine | Dapper 2.1.66 (for measured complex analytical queries & aggregation) + EF Core |
| Database | SQL Server 2022 (Docker: `mcr.microsoft.com/mssql/server:2022-latest`) |
| Message Broker | RabbitMQ 3.x (Docker: `rabbitmq:3-management`) — client: `RabbitMQ.Client v7` |
| Scheduler | Quartz.NET (in `ResolveOps.Worker`) |
| Realtime WebSockets | ASP.NET Core SignalR (`NotificationHub`) |
| Email Service | `IEmailSender` abstraction (Mailpit SMTP for local development: port 1025; Brevo/SendGrid configurable for production) |
| Business Calendar | `IBusinessCalendarService` (tenant-scoped working hours & holiday calculation) |
| Object Storage | MinIO (Docker: `minio/minio`) — client: `AWSSDK.S3` (S3-compatible presigned URLs for export downloads) |
| Cache | Redis 7 (Docker: `redis:7-alpine`) — client: `StackExchange.Redis` |
| Observability | OpenTelemetry + Serilog + Seq (`datalust/seq`) |
| Local Orchestration | .NET Aspire + Docker Compose |
| Frontend | Angular 19+ + TypeScript + Angular Material (Phase 15+) |
| Testing | Architecture tests (`ResolveOps.ArchitectureTests`) |
| Concurrency | Optimistic Concurrency via `string ConcurrencyStamp` (ADR-006 & centralized `AppDbContext`) |

**Rejected (do not add):** Azure proprietary services, MassTransit, AutoMapper, generic repositories, React/Vite, microservices, Kubernetes, Kafka.

## Current Implementation State

**Current phase:** `Phase 14 — Reporting and carrier scorecards`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA), Phase 9 (evidence and secure document pipeline), Phase 10 (claim eligibility and draft claims), Phase 11 (claim approval, submission, response, appeal), Phase 12 (financial recovery and settlement), Phase 13 (notifications and realtime operations)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings across all 29 projects).
- Architecture tests pass (5 passed, 0 failed).
- Code formatting passes strict verification (dotnet format ResolveOps.slnx --verify-no-changes exits with code 0).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Strict 1-class-per-file convention enforced across all vertical slices (<Feature>Command.cs, <Feature>Endpoint.cs, <Feature>Handler.cs, <Feature>Validator.cs, <Feature>Response.cs). Do NOT combine multiple classes into one file.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (AddHandlersFromAssembly, MapEndpointsFromAssembly). Endpoints implement IEndpoint.
- Standardized User & Tenant resolution: Endpoints MUST use httpContext.GetUserId() or httpContext.TryGetUserId() and httpContext.GetTenantId() from ResolveOps.Security extensions. Do NOT manually parse ClaimTypes.NameIdentifier or sub.
- Domain Constants: All domain statuses and types MUST be defined as public static class with public const string fields, NOT C# enums.
- Global Tenant Query Filters: AppDbContext centrally handles tenant isolation via HasQueryFilter. Handlers MUST NOT append manual .Where(x => x.TenantId == tenantId) filters.
- Observability Location: All metrics classes (e.g. ReportingMetrics.cs, NotificationMetrics.cs, ClaimMetrics.cs) MUST be placed in src/BuildingBlocks/ResolveOps.Observability, NOT inside module projects.
- Concurrency Stamp: Centralized optimistic concurrency via string ConcurrencyStamp in AppDbContext.ApplyAuditAndConcurrency() (domain entity methods MUST NOT manually mutate or roll stamps).
- Database schema: Migrations through 20260922160422_AddNotificationsAndRealtimeOperations are applied.
- Background workers running in ResolveOps.Worker:
  - TrackingIngestionConsumerService (RabbitMQ v7 async consumer for raw carrier receipts)
  - ExceptionEvaluationConsumerService (RabbitMQ v7 async consumer for TrackingEventAcceptedV1)
  - NotificationConsumerService (RabbitMQ v7 async consumer with transient retry & DLX for operational alerts)
  - MissedDeadlineScanJob (Quartz.NET periodic scan for missed shipment milestones)
  - SlaBreachScanJob (Quartz.NET periodic scan for breached SLA clocks)
  - ClaimFollowUpScanJob (Quartz.NET periodic scan for SLA follow-up deadlines)
  - NotificationEmailRetryScanJob (Quartz.NET periodic scan every 2m retrying pending email deliveries)
  - DocumentProcessingWorker (BackgroundService for document malware scanning)
  - AbandonedUploadCleanupJob (Quartz.NET periodic cleanup of expired upload intents)
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL UT/IT test requirements are currently waived. Architecture tests remain strictly required.
```

**Stopping point / specific task this session:**
```
Implement Phase 14: Reporting and carrier scorecards according to Master Spec §24 Phase 14, §0 (Rule 10), §4.1, §4.2, §10.6, §11, §12.2–12.4, §15, §16.12, §17.3, §17.4, §18.1, §19.9, §21, §22.7, §25:

1. Domain & Read Models (Spec §15, §24 Phase 14):
   In src/BuildingBlocks/ResolveOps.Domain/Reporting/:
   - ExportStatus (public static class with const string, NOT enums):
     - Pending = "Pending", Processing = "Processing", Completed = "Completed", Failed = "Failed". Provide IReadOnlyList<string> All, bool IsValid(string? status).
   - ExportType (public static class with const string, NOT enums):
     - ExceptionCases = "ExceptionCases", Claims = "Claims", CarrierScorecards = "CarrierScorecards". Provide IReadOnlyList<string> All, bool IsValid(string? type).
   - ExportRequest (Entity implementing IAuditableEntity):
     - Fields: Id (Guid), TenantId (Guid), UserId (Guid), ExportType (string max 50), Status (string max 30), FilterCriteriaJson (string? nvarchar(max)), Container (string max 100), BlobPath (string max 500), RowCount (int?), FileSizeBytes (long?), ErrorMessage (string? nvarchar(max)), CompletedAtUtc (DateTimeOffset?), CreatedAtUtc (DateTimeOffset), ConcurrencyStamp, IAuditableEntity properties.
     - Methods:
       - Create(Guid tenantId, Guid userId, string exportType, string? filterCriteriaJson, TimeProvider timeProvider)
       - MarkProcessing(TimeProvider timeProvider)
       - MarkCompleted(string container, string blobPath, int rowCount, long fileSizeBytes, TimeProvider timeProvider)
       - MarkFailed(string errorMessage, TimeProvider timeProvider)
   - CarrierPerformanceSnapshot (Read Model / Aggregation Entity):
     - Fields: Id (Guid), TenantId (Guid), CarrierId (Guid), CarrierName (string max 200), PeriodDate (DateOnly), TotalShipments (int), OnTimeShipments (int), DelayedShipments (int), ExceptionCount (int), CriticalSeverityCount (int), HighSeverityCount (int), MediumSeverityCount (int), LowSeverityCount (int), TotalClaims (int), ApprovedClaims (int), RejectedClaims (int), TotalClaimedAmount (decimal precision 19,4), TotalApprovedAmount (decimal precision 19,4), TotalRecoveredAmount (decimal precision 19,4), AvgResponseTimeHours (double), LastCalculatedAtUtc (DateTimeOffset).

2. CSV Formula-Injection Protection & Export Pipeline (Spec §19.9, §22.7, §24 Phase 14):
   In src/BuildingBlocks/ResolveOps.Application/Reporting/ or ResolveOps.Modules.Reporting/Services/:
   - CsvFormulaEscaper:
     - Escapes any text value starting with '=', '+', '-', '@', '\t', '\r' by prefixing with a single quote '\''.
     - Correctly quotes and escapes internal double quotes (" -> "").
   - Asynchronous Export Service / Pipeline:
     - Asynchronous queueing: POST /api/exports/exception-cases validates filters, creates ExportRequest in Pending status, and returns 202 Accepted with export ID and poll URI.
     - Background export execution via Quartz.NET job or BackgroundService:
       - Streams data from database in batches.
       - Generates formula-safe CSV content.
       - Uploads to MinIO container "exports" via IObjectStorageService.
       - Updates ExportRequest status to Completed with row count and file size.
     - Authorized download: GET /api/exports/{exportId} checks tenant isolation and user authorization, returns status and a short-lived presigned GET download URL (valid 15-30 mins) once ready.

3. Reporting Endpoints & Dashboards (Spec §16.12, §24 Phase 14):
   In src/Modules/ResolveOps.Modules.Reporting/Features/:
   Strictly adhere to Vertical Slice Architecture with 1 class per file (Endpoint.cs, Handler.cs, Query.cs/Command.cs, Validator.cs, Response.cs):
   - Operations Dashboard:
     - GET /api/dashboard/operations (and alias /dashboard/operations)
     - Aggregates: Open exception cases by severity (Critical, High, Medium, Low), SLA breach count / at-risk count (nearing breach within 2 hours), unassigned tasks count, active carrier delay rate, today's ingested tracking events vs exceptions detected.
   - Claims Dashboard:
     - GET /api/dashboard/claims (and alias /dashboard/claims)
     - Aggregates: Draft claims count, under-review claims count, submitted claims count, disputed claims count, total claimed amount, total approved amount, total recovered amount, recovery rate percentage (TotalRecovered / TotalApproved), average time to carrier decision.
   - Exception Ageing Report:
     - GET /api/reports/exception-ageing (and alias /reports/exception-ageing)
     - Query parameters: CarrierId, Severity, FromDate, ToDate.
     - Aggregates cases into ageing buckets: 0-24h, 24-48h, 48-72h, 3-7d, >7d, grouped by exception type and carrier.
   - SLA Performance Report:
     - GET /api/reports/sla-performance (and alias /reports/sla-performance)
     - Query parameters: CarrierId, Priority, FromDate, ToDate.
     - Aggregates: Total SLA clocks tracked, met vs breached rate percentage, average time to triage, average time to resolution, compliance percentage by carrier.
   - Carrier Scorecard Report:
     - GET /api/reports/carrier-scorecards (and alias /reports/carrier-scorecards)
     - Query parameters: CarrierId, FromDate, ToDate.
     - Metrics per carrier:
       - Shipment count
       - Exception rate (%)
       - On-time rate (%)
       - Severity distribution (Critical, High, Medium, Low)
       - Average response time (hours)
       - Claim approval rate (%)
       - Recovery rate (%)
   - Financial Recovery Report:
     - GET /api/reports/financial-recovery (and alias /reports/financial-recovery)
     - Query parameters: CarrierId, FromDate, ToDate.
     - Aggregates: Recovery breakdown by carrier and recovery type (Payment, CreditNote, Adjustment), total written-off amount with reasons, pending recovery balance.
   - Asynchronous Export Endpoints:
     - POST /api/exports/exception-cases (and alias /exports/exception-cases): Initiates async export, returns 202 Accepted.
     - GET /api/exports/{exportId:guid} (and alias /exports/{exportId:guid}): Returns export status, row count, file size, and presigned download URL when completed.

4. High-Performance Querying & Dapper / EF Core (Spec §12.4, §24 Phase 14):
   - Use Dapper (package version 2.1.66 centrally pinned) for measured complex analytical queries and aggregations where Dapper provides superior performance.
   - Ensure all queries are strictly tenant-isolated by filtering with `@TenantId`.
   - Update ResolveOps.Modules.Reporting.csproj to reference Dapper and necessary building blocks.

5. Background Workers (Spec §18.1, §24 Phase 14):
   In src/Hosts/ResolveOps.Worker/:
   - ReportingProjectionConsumerService (RabbitMQ Consumer):
     - Subscribes to queue `resolveops.reporting` bound to exchange `resolveops.events`.
     - Monitored exchanges / events:
       - `ShipmentCreatedV1`
       - `TrackingEventAcceptedV1`
       - `ExceptionDetectedV1`
       - `CaseSlaBreachedV1`
       - `ClaimSubmittedV1`
       - `ClaimDecisionRecordedV1`
       - `ClaimRecoveryRecordedV1`
       - `EvidenceAvailableV1`
     - Enforces transactional inbox deduplication (`InboxMessage`).
     - Updates carrier performance daily aggregation snapshots idempotently.
   - ExportProcessingJob (Quartz.NET Job running every 30 seconds or 1 minute):
     - Scans `ExportRequest` records where `Status == ExportStatus.Pending`.
     - Streams records from database, applies `CsvFormulaEscaper`, generates CSV file.
     - Uploads file to MinIO bucket `exports` via `IObjectStorageService`.
     - Updates `ExportRequest` status to `Completed` with file metrics.
     - Rule 10: External object storage uploads executed outside database transactions.

6. Persistence & EF Core Configuration (Spec §15):
   In src/BuildingBlocks/ResolveOps.Persistence/Configurations/:
   - ExportRequestConfiguration:
     - Table `export_requests`. Index on `(tenant_id, user_id, status, created_at_utc)`.
   - CarrierPerformanceSnapshotConfiguration:
     - Table `carrier_performance_snapshots`. Unique index on `(tenant_id, carrier_id, period_date)`.
   Update AppDbContext:
   - Add DbSets: `ExportRequests`, `CarrierPerformanceSnapshots`.
   - Add global tenant query filters for each entity.
   - Generate EF Core Migration: `AddReportingAndCarrierScorecards`.

7. Observability & Metrics (Spec §21):
   In src/BuildingBlocks/ResolveOps.Observability/ReportingMetrics.cs:
   - `reporting.queries.duration.seconds` (Histogram<double>, tags: report_name)
   - `reporting.exports.total` (Counter<long>, tags: export_type, status)
   - `reporting.exports.duration.seconds` (Histogram<double>, tags: export_type)
   - `reporting.projections.processed.total` (Counter<long>, tags: event_type)

8. Architecture Decisions & Documentation:
   - Create `docs/adr/ADR-031-reporting-and-carrier-scorecards.md`.
   - Update `AGENTS.md` (Current Phase: Phase 14 Complete, Next: Phase 15) and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 14 from the specification.
3. **Implement only the Phase 14 scope** — do not add features from future phases (e.g., frontend in Phase 15, stress testing in Phase 16).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Reporting`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`, `ResolveOps.Security`, `ResolveOps.Worker`).
5. **Enforce 1-class-per-file**: Every vertical slice feature MUST have separate files: `<Action><Resource>Endpoint.cs`, `<Action><Resource>Handler.cs`, `<Action><Resource>Query.cs` / `Command.cs`, `<Action><Resource>Validator.cs`, `<Action><Resource>Response.cs`.
6. **Use centralized security extensions**: Use `httpContext.GetUserId()` / `httpContext.GetTenantId()` in all endpoints. Do NOT manually extract claim strings.
7. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
8. **Ensure ConcurrencyStamp integrity**: Concurrency stamps are managed centrally by `AppDbContext.ApplyAuditAndConcurrency()`; domain methods MUST NOT manually mutate stamps.
9. **Add/update** EF Core migration `AddReportingAndCarrierScorecards`, OpenTelemetry instrumentation in `ResolveOps.Observability`, and ADR-031.
10. **Run** formatting (`dotnet format ResolveOps.slnx --verify-no-changes`), build (`dotnet build ResolveOps.slnx -c Release`), and architecture tests (`dotnet test tests/ResolveOps.ArchitectureTests/ -c Release`).
11. **Fix** any failures caused by your changes before reporting done.
12. **Update** `CHANGELOG.md` and `AGENTS.md` with current phase status.
13. **Report** at the end: files changed, commands run, test results, assumptions made, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT allow an LLM or AI to approve, reject, pay, or execute financial transitions on claims (Rule 4 & Invariant 12).
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT use AutoMapper — mapping must be explicit.
- Do NOT put business logic in endpoints.
- Do NOT use database transactions around file uploads, message publication, or external HTTP calls (Rule 10).
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Do NOT use `float` or `double` for monetary values — always use `decimal` or the `Money` value object.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests remain mandatory.

## Definition of Done Checklist (Section 31 & §24 Phase 14)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] All features follow strict 1-class-per-file convention in `ResolveOps.Modules.Reporting`
- [ ] Domain constants defined as `public static class` with `public const string` constants (NO enums)
- [ ] `ExportRequest` entity implemented with guarded factory & state transition methods
- [ ] `CarrierPerformanceSnapshot` read model implemented for high-performance scorecards
- [ ] CSV formula-injection protection implemented (`=`, `+`, `-`, `@`, `\t`, `\r` escaped with `'`)
- [ ] Operations Dashboard (`GET /api/dashboard/operations`) implemented with real-time operational aggregates
- [ ] Claims Dashboard (`GET /api/dashboard/claims`) implemented with financial recovery and status breakdown
- [ ] Exception Ageing Report (`GET /api/reports/exception-ageing`) implemented with age bucket distributions
- [ ] SLA Performance Report (`GET /api/reports/sla-performance`) implemented with compliance and resolution metrics
- [ ] Carrier Scorecard Report (`GET /api/reports/carrier-scorecards`) implemented covering all 7 spec metrics:
  - shipment count
  - exception rate
  - on-time rate
  - severity distribution
  - average response time
  - claim approval rate
  - recovery rate
- [ ] Financial Recovery Report (`GET /api/reports/financial-recovery`) implemented with carrier breakdown and write-off analysis
- [ ] Asynchronous CSV export (`POST /api/exports/exception-cases` and `GET /api/exports/{exportId}`) implemented
- [ ] Asynchronous exports uploaded to MinIO with temporary presigned download URLs
- [ ] Export processing worker executed outside database transactions (Rule 10)
- [ ] `ReportingProjectionConsumerService` in `ResolveOps.Worker` subscribes to operational events and updates read models idempotently
- [ ] All endpoints use `httpContext.GetUserId()` / `httpContext.GetTenantId()` from `ResolveOps.Security`
- [ ] Module auto-discovery preserved (`AddHandlersFromAssembly`, `MapEndpointsFromAssembly`)
- [ ] Handlers rely on `AppDbContext` global tenant filter (no redundant manual `.Where(x => x.TenantId == tenantId)`)
- [ ] EF Core configurations and migration `AddReportingAndCarrierScorecards` applied
- [ ] Metrics instrumented in `src/BuildingBlocks/ResolveOps.Observability/ReportingMetrics.cs`
- [ ] ADR-031 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Giá trị cho Phase 14 |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 14 — Reporting and carrier scorecards` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0 through Phase 13 (Notifications and realtime operations)` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Clean build Release 0 errors/warnings, ArchTests 5/5 pass, EF migration `20260922160422_AddNotificationsAndRealtimeOperations` |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai Operations & Claims dashboards, Exception ageing, SLA performance, Carrier scorecards, Financial recovery report, Asynchronous CSV exports with formula-injection escaping, MinIO presigned download URLs, ReportingProjectionConsumerService, and ExportProcessingJob |

# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 11.

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
- Section 0: Agent rules and non-negotiable constraints (Rule 4: AI is strictly forbidden from financial state transitions or approving/rejecting claims)
- Section 8.8: Claim review and submission preparation (Step 6: ReadyForReview -> Step 7: Authorized reviewer approves submission -> Step 8: Submission package generated -> Step 9: User submits externally or integration sends -> Step 10: Submission reference & timestamp recorded -> Step 11: Claim moves to Submitted -> Step 12: Follow-up SLA scheduled)
- Section 8.9: Carrier claim response & appeal workflow (Decisions: `Acknowledged`, `MoreInformationRequested`, `Approved`, `PartiallyApproved`, `Denied`, `SettlementOffered`; response metadata: carrier reference, response date, approved amount, reason codes, attached docs, recorded by, source channel)
- Section 9.2: Claim states & state machine transitions (`ReadyForReview` -> `ApprovedForSubmission`, `ReadyForReview` -> `Draft`, `ApprovedForSubmission` -> `Submitted`, `Submitted` -> `Acknowledged`, `Submitted` -> `MoreInformationRequested`, `Acknowledged`/`MoreInformationRequested` -> `UnderReview`, `UnderReview` -> `Approved`/`PartiallyApproved`/`Denied`, `Denied`/`PartiallyApproved` -> `Appealed` -> `UnderReview`)
- Section 10.5: Claim invariants (Invariant 4: approved amount cannot be negative; Invariant 6: claim cannot submit without required evidence, submission approval, and non-expired deadline; Invariant 7: submitted claim cannot silently change claimed amount; Invariant 10: unique external submission reference; Invariant 12: AI cannot execute financial state transitions)
- Section 10.6: Tenant invariants (tenant isolation on all queries and commands via `AppDbContext` global query filter; do NOT add redundant manual `.Where(x => x.TenantId == tenantId)`)
- Section 11: Edge cases (Edge Case 16: partial approval retains denied difference; Edge Case 24: claim submitted near deadline; Edge Case 25: carrier responds after write-off)
- Section 12: Architecture and module structure (`ResolveOps.Modules.Claims`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`)
- Section 15.10: Claim tables (`claims`, `claim_approvals`, `carrier_claim_responses`)
- Section 16.10: Claim endpoints (`POST /claims/{claimId}/request-review`, `POST /claims/{claimId}/approve-for-submission`, `POST /claims/{claimId}/return-to-draft`, `POST /claims/{claimId}/record-submission`, `GET /claims/{claimId}/submission-package`, `POST /claims/{claimId}/record-acknowledgement`, `POST /claims/{claimId}/record-information-request`, `POST /claims/{claimId}/supply-additional-information`, `POST /claims/{claimId}/record-decision`, `POST /claims/{claimId}/appeal`, `GET /claims/{claimId}/timeline`, `POST /claims/{claimId}/cancel`)
- Section 17.3: Outbox integration events (`ClaimSubmittedV1`, `ClaimDecisionRecordedV1`)
- Section 24: Implementation roadmap and Phase 11 definition
- Section 25: Coding standards (C# 13 / .NET 10, explicit mapping, no generic repo, 1 class per file)
- Section 26.10: Separation of duties (preparer cannot approve own claim if claimed amount exceeds tenant threshold, e.g. 10,000,000 VND)
- Section 26.11: Partial approval reconciliation (approved amount + denied difference = claimed amount)
- `AGENTS.md` in the repository root and `docs/adr/ADR-006-auto-discovery-and-concurrency.md`

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

**Current phase:** `Phase 11 — Claim approval, submission, response, appeal`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA), Phase 9 (evidence and secure document pipeline), Phase 10 (claim eligibility and draft claims)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings).
- Architecture tests pass (5 passed, 0 failed).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Strict 1-class-per-file convention enforced across all vertical slices (<Feature>Command.cs, <Feature>Endpoint.cs, <Feature>Handler.cs, <Feature>Validator.cs, <Feature>Response.cs). Do NOT combine multiple classes into one file.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (AddHandlersFromAssembly, MapEndpointsFromAssembly). Do NOT manually map endpoints or handlers.
- Domain Constants: All domain statuses and types (ClaimStatus, ClaimType, ClaimEligibilityStatus, LossComponentType, etc.) MUST be defined as public static class with public const string fields, NOT C# enums.
- Global Tenant Query Filters: AppDbContext centrally handles tenant isolation via HasQueryFilter. Handlers MUST NOT append manual .Where(x => x.TenantId == tenantId) filters.
- Observability Location: All metrics classes (e.g. ClaimMetrics.cs) MUST be placed in src/BuildingBlocks/ResolveOps.Observability, NOT inside module projects.
- Concurrency Stamp: Centralized optimistic concurrency via string ConcurrencyStamp in AppDbContext.ApplyAuditAndConcurrency() (domain entity methods MUST NOT manually mutate or roll stamps).
- Database schema: Migrations through 20260919161858_AddClaimsAndEligibilityEngine are applied.
- Background workers running in ResolveOps.Worker via Quartz.NET:
  - TrackingIngestionConsumerService (RabbitMQ v7 async consumer for raw carrier receipts)
  - ExceptionEvaluationConsumerService (RabbitMQ v7 async consumer for TrackingEventAcceptedV1)
  - MissedDeadlineScanJob (Quartz.NET 1-min periodic scan for missed shipment milestones)
  - SlaBreachScanJob (Quartz.NET 1-min periodic scan for breached SLA clocks emitting CaseSlaBreachedV1)
  - DocumentProcessingScanJob (Quartz.NET 10-s periodic scan for pending document malware verification)
  - AbandonedUploadCleanupJob (Quartz.NET 6-hour periodic cleanup of expired upload intents)
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL test requirements are currently waived. Architecture tests remain strictly required.
```

**Stopping point / specific task this session:**
```
Implement Phase 11: Claim approval, submission, response, appeal according to Master Spec §24 Phase 11, §8.8, §8.9, §9.2, §10.5, §10.6, §11 (Edge Case 16, 24, 25), §12, §15.10, §16.10, §17.3, §25, §26.10, §26.11:

1. Domain Entities & Value Objects (Spec §8.8, §8.9, §15.10):
   - In `src/BuildingBlocks/ResolveOps.Domain/Claims/`:
     - `ClaimApproval`:
       - Fields: `Id`, `TenantId`, `ClaimId`, `ApprovalType` (Submission, WriteOff), `Status` (Pending, Approved, Rejected), `RequestedBy` (Guid), `RequestedAtUtc` (DateTimeOffset), `DecidedBy` (Guid?), `DecidedAtUtc` (DateTimeOffset?), `DecisionNote` (string?), `ClaimVersion` (string/long concurrency snapshot).
     - `CarrierClaimResponse`:
       - Fields: `Id`, `TenantId`, `ClaimId`, `ResponseType` (Acknowledged, MoreInformationRequested, Approved, PartiallyApproved, Denied, SettlementOffered), `CarrierReference` (string?), `ResponseAtUtc` (DateTimeOffset), `ApprovedAmount` (decimal(19,4)?), `Currency` (string? 3-char ISO), `ReasonCodes` (string[] or JSON string), `Notes` (string?), `RecordedBy` (Guid), `SourceChannel` (Email, Portal, EDI, Api, Manual), `CreatedAtUtc` (DateTimeOffset).
       - Invariant (§24): Carrier response history is strictly immutable (append-only).
     - Domain Constants (MUST use `public static class` with `public const string` fields, NOT enums):
       - `ApprovalStatus`: `Pending`, `Approved`, `Rejected`
       - `ApprovalType`: `Submission`, `WriteOff`
       - `CarrierResponseType`: `Acknowledged`, `MoreInformationRequested`, `Approved`, `PartiallyApproved`, `Denied`, `SettlementOffered`
       - `SourceChannel`: `Email`, `Portal`, `EDI`, `Api`, `Manual`
       - `ClaimStatus` verify/include: `Draft`, `EvidencePending`, `ReadyForReview`, `ApprovedForSubmission`, `Submitted`, `Acknowledged`, `MoreInformationRequested`, `UnderReview`, `Approved`, `PartiallyApproved`, `Denied`, `Appealed`, `SettlementPending`, `Paid`, `Closed`, `Cancelled`.
     - `Claim` Aggregate Root Domain Method Enhancements:
       - `RequestReview(Guid requestedBy)`:
         - Validates claimed amount > 0, claim status in (`Draft`, `EvidencePending`), eligibility is `Eligible` or `ConditionallyEligible`, no missing mandatory evidence.
         - Moves status to `ReadyForReview`. Adds/updates pending `ClaimApproval`.
       - `ApproveForSubmission(Guid approverId, decimal highValueThreshold, string? note)`:
         - Enforces Separation of Duties (§26.10): If `ClaimedAmount >= highValueThreshold`, approver cannot be the same user who created/prepared the claim (`CreatedBy` / `RequestedBy`).
         - Guards status == `ReadyForReview`. Sets `ApprovedForSubmissionBy = approverId`, `ApprovedForSubmissionAtUtc = utcNow`, updates `ClaimApproval` status to `Approved`. Moves status to `ApprovedForSubmission`.
       - `ReturnToDraft(Guid reviewerId, string reason)`:
         - Guards status == `ReadyForReview`. Updates `ClaimApproval` to `Rejected` with `reason`. Moves status back to `Draft`.
       - `RecordSubmission(string externalReference, DateTimeOffset submittedAtUtc, DateTimeOffset currentUtc)`:
         - Guards status == `ApprovedForSubmission`.
         - Invariant 6 & Edge Case 24: Validates submission date does not exceed `ClaimDeadlineAtUtc`.
         - Sets `ExternalSubmissionReference`, `SubmittedAtUtc`. Moves status to `Submitted`.
         - Emits `ClaimSubmittedV1` outbox event.
       - `RecordAcknowledgement(string? carrierReference, DateTimeOffset responseAtUtc, string? notes, Guid recordedBy, string sourceChannel)`:
         - Adds `CarrierClaimResponse`. Moves status from `Submitted` to `Acknowledged` (or `UnderReview`).
       - `RecordInformationRequest(string? carrierReference, DateTimeOffset responseAtUtc, string? notes, string[] reasonCodes, DateTimeOffset infoDeadlineAtUtc, Guid recordedBy, string sourceChannel)`:
         - Adds `CarrierClaimResponse`. Moves status to `MoreInformationRequested`.
       - `SupplyAdditionalInformation(string responseNotes, Guid recordedBy)`:
         - Guards status == `MoreInformationRequested`. Moves status to `UnderReview`.
       - `RecordDecision(string decisionType, decimal? approvedAmount, string[] reasonCodes, string? carrierReference, string? notes, DateTimeOffset responseAtUtc, Guid recordedBy, string sourceChannel)`:
         - Adds immutable `CarrierClaimResponse`.
         - Decision rules (§26.11):
           - `Approved`: `ApprovedAmount = ClaimedAmount`. Status -> `Approved`.
           - `PartiallyApproved`: `0 < ApprovedAmount < ClaimedAmount`. Retains denied difference (`ClaimedAmount - ApprovedAmount`). Status -> `PartiallyApproved`.
           - `Denied`: `ApprovedAmount = 0`. Status -> `Denied`.
         - Invariant: Approved amount cannot be negative and cannot exceed claimed amount.
         - Emits `ClaimDecisionRecordedV1` outbox event.
       - `Appeal(string appealReason, string? notes, Guid appealedBy)`:
         - Guards status in (`Denied`, `PartiallyApproved`).
         - Moves status to `Appealed` (or back to `UnderReview`).
       - `Cancel(string reason, Guid cancelledBy)`:
         - Guards claim status not in (`Submitted`, `Approved`, `SettlementPending`, `Paid`, `Closed`). Moves status to `Cancelled`.

2. Outbox Integration Events (Spec §17.3):
   - In `src/BuildingBlocks/ResolveOps.Domain/Claims/Events/`:
     - `ClaimSubmittedV1`:
       - Fields: `Guid ClaimId`, `string ClaimNumber`, `Guid CaseId`, `Guid CarrierId`, `decimal ClaimedAmount`, `string Currency`, `DateTimeOffset SubmittedAtUtc`, `DateTimeOffset DeadlineAtUtc`.
     - `ClaimDecisionRecordedV1`:
       - Fields: `Guid ClaimId`, `string Decision`, `decimal ApprovedAmount`, `string Currency`, `DateTimeOffset RecordedAtUtc`.
   - Publish to `outbox_messages` atomically within `AppDbContext.SaveChangesAsync()`.

3. Submission Package & Claim Timeline (Spec §8.8, §16.10, §24 Task 5):
   - `SubmissionPackage`:
     - Service / feature returning submission package metadata: claim header, case information, carrier detail, breakdown of loss components, and presigned access / references to verified evidence documents.
   - `ClaimTimeline`:
     - Feature querying unified history of the claim: draft creation, evidence readiness updates, review request, submission approval, carrier submission, acknowledgements, info requests, decisions, and appeals.

4. Claim REST APIs (`ResolveOps.Modules.Claims`) (Spec §16.10):
   - MUST strictly adhere to **Vertical Slice Architecture** with **1 class per file** (<Feature>Endpoint.cs, <Feature>Handler.cs, <Feature>Command.cs/Query.cs, <Feature>Validator.cs, <Feature>Response.cs):
     - `RequestReview`: `POST /api/claims/{claimId}/request-review`
     - `ApproveForSubmission`: `POST /api/claims/{claimId}/approve-for-submission` (Accepts `notes`, verifies separation of duties policy against tenant threshold).
     - `ReturnToDraft`: `POST /api/claims/{claimId}/return-to-draft` (Accepts `reason`).
     - `RecordSubmission`: `POST /api/claims/{claimId}/record-submission` (Accepts `externalReference`, `submittedAtUtc`).
     - `GetSubmissionPackage`: `GET /api/claims/{claimId}/submission-package`
     - `RecordAcknowledgement`: `POST /api/claims/{claimId}/record-acknowledgement`
     - `RecordInformationRequest`: `POST /api/claims/{claimId}/record-information-request`
     - `SupplyAdditionalInformation`: `POST /api/claims/{claimId}/supply-additional-information`
     - `RecordDecision`: `POST /api/claims/{claimId}/record-decision` (Accepts `decision`, `approvedAmount`, `carrierReference`, `reasonCodes`, `notes`).
     - `AppealClaim`: `POST /api/claims/{claimId}/appeal` (Accepts `appealReason`, `notes`).
     - `GetClaimTimeline`: `GET /api/claims/{claimId}/timeline`
     - `CancelClaim`: `POST /api/claims/{claimId}/cancel`

5. Persistence & EF Core Configuration:
   - EF Core configurations in `src/BuildingBlocks/ResolveOps.Persistence/Configurations/`:
     - `ClaimApprovalConfiguration`: Table `claim_approvals`, foreign key to `claims`, tenant filter.
     - `CarrierClaimResponseConfiguration`: Table `carrier_claim_responses`, foreign key to `claims`, precision `approved_amount decimal(19,4)`, tenant filter.
     - Update `ClaimConfiguration`: Add navigation collections `Approvals` and `Responses`.
   - Update `AppDbContext`: Add `DbSet<ClaimApproval> ClaimApprovals` and `DbSet<CarrierClaimResponse> CarrierClaimResponses`.
   - EF Core Migration: `AddClaimApprovalSubmissionAndResponses`.

6. Background Worker in `ResolveOps.Worker` (Quartz.NET):
   - `ClaimFollowUpScanJob`:
     - Quartz periodic scan (e.g. every 15 minutes) checking:
       - Claims in `Submitted` or `UnderReview` approaching follow-up response SLA.
       - Claims in `MoreInformationRequested` approaching additional information deadline.
       - Logs follow-up alerts / generates operational reminder tasks.

7. Observability & Documentation:
   - In `src/BuildingBlocks/ResolveOps.Observability/ClaimMetrics.cs`:
     - `claims.approved.total`, `claims.submitted.total`, `claims.decisions.recorded.total`, `claims.appealed.total`, `claims.amount.approved.total`.
   - Create `docs/adr/ADR-028-claim-approval-submission-and-response.md`.
   - Update `AGENTS.md` and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user).
Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 11 from the specification.
3. **Implement only the Phase 11 scope** — do not add features from future phases (e.g., financial recovery/settlement in Phase 12, notifications/SignalR in Phase 13).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Claims`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`).
5. **Enforce 1-class-per-file**: Every vertical slice feature MUST have separate files: `<Action><Resource>Endpoint.cs`, `<Action><Resource>Handler.cs`, `<Action><Resource>Command.cs` / `Query.cs`, `<Action><Resource>Validator.cs`, `<Action><Resource>Response.cs`.
6. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
7. **Ensure ConcurrencyStamp integrity**: Concurrency stamps are managed centrally by `AppDbContext.ApplyAuditAndConcurrency()`; domain methods MUST NOT manually mutate stamps.
8. **Add/update** EF Core migrations, OpenTelemetry instrumentation in `ResolveOps.Observability`, and ADR-028.
9. **Run** formatting (`dotnet format`), build (`dotnet build ResolveOps.slnx -c Release`), and architecture tests (`dotnet test tests/ResolveOps.ArchitectureTests/ -c Release`).
10. **Fix** any failures caused by your changes before reporting done.
11. **Update** `CHANGELOG.md` and `AGENTS.md` with current phase status.
12. **Report** at the end: files changed, commands run, test results, assumptions made, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT allow an LLM or AI to approve, reject, or execute financial transitions on claims (Rule 4).
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT use AutoMapper — mapping must be explicit.
- Do NOT put business logic in endpoints.
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Do NOT use `float` or `double` for monetary values — always use `decimal` or the `Money` value object.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests remain mandatory.

## Definition of Done Checklist (Section 31 & §24 Phase 11)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] All features follow strict 1-class-per-file convention in `ResolveOps.Modules.Claims`
- [ ] Domain constants defined as `public static class` with `public const string` constants (NO enums)
- [ ] `ClaimApproval` and `CarrierClaimResponse` entities implemented with guarded business methods
- [ ] Carrier response history is immutable (append-only)
- [ ] `Claim` aggregate root state machine transitions correctly handle review, approval, submission, responses, and appeals
- [ ] Separation of duties strictly enforced: claim preparer cannot approve own claim when amount exceeds tenant threshold
- [ ] Partial approval retains denied difference (`ClaimedAmount - ApprovedAmount`)
- [ ] Claim submission is blocked without required evidence, submission approval, or if deadline expired
- [ ] Outbox integration events `ClaimSubmittedV1` and `ClaimDecisionRecordedV1` published atomically
- [ ] `GET /api/claims/{claimId}/submission-package` generates complete submission metadata and evidence links
- [ ] `GET /api/claims/{claimId}/timeline` returns chronological timeline of claim lifecycle
- [ ] REST API endpoints in `ResolveOps.Modules.Claims` implemented with Minimal APIs & FluentValidation
- [ ] Module auto-discovery preserved (`AddHandlersFromAssembly`, `MapEndpointsFromAssembly`)
- [ ] Handlers rely on `AppDbContext` global tenant filter (no redundant manual `.Where(x => x.TenantId == tenantId)`)
- [ ] EF Core configurations, decimal(19,4) precision, and migration applied
- [ ] `ClaimFollowUpScanJob` configured in `ResolveOps.Worker` via Quartz.NET
- [ ] Metrics instrumented in `src/BuildingBlocks/ResolveOps.Observability/ClaimMetrics.cs`
- [ ] ADR-028 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Ví dụ |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 11 — Claim approval, submission, response, appeal` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Số migration, số test, file nào đang có |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai Claim approval, separation of duties, submission package, carrier responses, appeals |

### Cách lấy repository state nhanh:

Chạy lệnh này trong repo để lấy thông tin điền vào:
```powershell
# Chạy trong thư mục repo
Write-Host "=== BUILD ===" ; dotnet build --no-restore -q 2>&1 | tail -3
Write-Host "=== ARCH TESTS ===" ; dotnet test tests/ResolveOps.ArchitectureTests/ --no-build -q 2>&1 | tail -5  
Write-Host "=== MIGRATIONS ===" ; dotnet ef migrations list --project src/BuildingBlocks/ResolveOps.Persistence 2>&1
Write-Host "=== GIT STATUS ===" ; git log --oneline -5
```

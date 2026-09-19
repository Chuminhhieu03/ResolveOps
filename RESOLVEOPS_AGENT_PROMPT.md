# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 10.

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
- Section 8.7: Claim eligibility & preparation workflow (deterministic eligibility inputs, reason codes, policy versions)
- Section 8.8: Claim review and submission preparation (loss components, claim amount calculation, readiness checks)
- Section 9.2: Claim states (`Draft`, `EvidencePending`, `ReadyForReview`, `ApprovedForSubmission`, `Submitted`, etc.)
- Section 10.5: Claim invariants (eligible case, compatible claim type, positive claimed amount, single currency, duplicate active claim prevention, AI forbidden from financial transitions)
- Section 10.6: Tenant invariants (tenant isolation on all queries, commands, unique indexes)
- Section 11: Edge cases (Edge Case 8: claim deadline falling on holidays/business calendar; Edge Case 9: currency consistency)
- Section 12: Architecture and module structure (`ResolveOps.Modules.Claims`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`)
- Section 15.10: Claim tables (`claims`, `claim_loss_components`)
- Section 16.10: Claim endpoints (`POST /exceptions/{caseId}/claims`, `GET /claims`, `GET /claims/{claimId}`, `POST /claims/{claimId}/loss-components`, `PUT /claims/{claimId}/loss-components/{componentId}`, `DELETE /claims/{claimId}/loss-components/{componentId}`, `POST /claims/{claimId}/calculate-eligibility`, readiness evaluation)
- Section 17.3: Outbox integration events (`EvidenceAvailableV1` triggering claim readiness evaluation)
- Section 24: Implementation roadmap and Phase 10 definition
- Section 25: Coding standards (C# 13 / .NET 10, explicit mapping, no generic repo, 1 class per file)
- `AGENTS.md` in the repository root and `docs/adr/ADR-006-domain-model-and-concurrency.md`

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

**Current phase:** `Phase 10 — Claim eligibility and draft claims`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA), Phase 9 (evidence and secure document pipeline)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings).
- Architecture tests pass (5 passed, 0 failed).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Strict 1-class-per-file convention enforced across all vertical slices (<Feature>Command.cs, <Feature>Endpoint.cs, <Feature>Handler.cs, <Feature>Validator.cs, <Feature>Response.cs). Do NOT combine multiple classes into one file.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (AddHandlersFromAssembly, MapEndpoints).
- DomainErrors unified via ErrorTemplates table and DatabaseErrorMessageProvider returning RFC 7807 Problem Details.
- Centralized optimistic concurrency via string ConcurrencyStamp in AppDbContext.ApplyAuditAndConcurrency() (domain methods do NOT manually assign or roll stamps).
- Database schema: Migrations through 20260919153000_UpdateEvidencePipelineInvariants are applied.
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
Implement Phase 10: Claim eligibility and draft claims according to Master Spec §24 Phase 10, §8.7, §8.8, §9.2, §10.5, §10.6, §11, §12, §15.10, §16.10, §25:

1. Claim Aggregate, Loss Component & Money Value Object (Spec §8.7, §10.5, §15.10):
   - Create domain entities in `src/BuildingBlocks/ResolveOps.Domain/Claims/`:
     - `Money` Value Object:
       - Fields: `decimal Amount`, `string Currency` (3-letter uppercase ISO, e.g. "USD", "VND", "EUR").
       - Methods: `Add(Money)`, `Subtract(Money)`, `Multiply(decimal factor)`, `IsZero()`, comparison operators (`==`, `!=`, `<`, `>`).
       - Invariants: Currency must match on arithmetic; Amount must be >= 0 for prices/components; NEVER use float or double.
     - `Claim` Aggregate Root:
       - Fields: `Id`, `TenantId`, `ClaimNumber` (human-readable string e.g. "CLM-2026-000001"), `CaseId`, `CarrierId`, `ClaimType` (CargoDamage, TotalLoss, Shortage, Delay), `Status` (Draft, EvidencePending, ReadyForReview, ApprovedForSubmission, Submitted, Cancelled - §9.2), `EligibilityStatus` (Eligible, ConditionallyEligible, NotEligible, InsufficientInformation - §8.7), `EligibilityReasonCodes` (JSON or string array), `PolicyVersionId`, `ClaimDeadlineAtUtc`, `ClaimedAmount` (`decimal(19,4)`), `ApprovedAmount` (`decimal(19,4)` default 0), `RecoveredAmount` (`decimal(19,4)` default 0), `Currency` (`nchar(3)`), `ExternalSubmissionReference`, `SubmittedAtUtc`, `ApprovedForSubmissionBy`, `ApprovedForSubmissionAtUtc`, `ClosedAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`, `ConcurrencyStamp`.
       - Collection: `LossComponents` (`IReadOnlyCollection<ClaimLossComponent>`).
       - Domain methods:
         - `CreateDraft(...)`: Factory method validating case eligibility, calculating filing deadline, setting initial status (`Draft` or `EvidencePending`).
         - `AddLossComponent(...)`: Validates currency matches claim currency, amount > 0, recalculates `ClaimedAmount`.
         - `UpdateLossComponent(...)`: Updates description, quantity, unit amount, total, recalculates `ClaimedAmount`.
         - `RemoveLossComponent(...)`: Removes component, recalculates `ClaimedAmount`.
         - `SetEligibility(status, reasonCodes, policyVersionId, deadlineUtc)`: Updates eligibility decision deterministically.
         - `UpdateReadiness(bool hasMissingMandatoryEvidence)`: Transitions between `Draft` and `EvidencePending` based on evidence readiness.
         - `MarkReadyForReview()`: Transitions to `ReadyForReview` if all validation passes (claimed amount > 0, no missing mandatory evidence, deadline not expired, eligibility is Eligible or ConditionallyEligible).
         - Invariants (§10.5): Claimed amount strictly equals the sum of validated loss components; positive claimed amount; single currency per claim; paid/closed claim immutable.
     - `ClaimLossComponent`:
       - Fields: `Id`, `TenantId`, `ClaimId`, `ComponentType` (FreightCharge, CargoValue, ReplacementCost, TaxOrFee, Repairs, Other), `Description`, `Quantity` (decimal nullable), `UnitAmount` (decimal nullable), `Amount` (decimal(19,4)), `Currency` (3-letter ISO), `SourceDocumentId` (nullable Guid referencing `EvidenceDocument`).
     - Enums/Constants: `ClaimType`, `ClaimStatus`, `ClaimEligibilityStatus`, `LossComponentType`.

2. Deterministic Claim Eligibility & Policy Engine (Spec §8.7, §10.5):
   - Define `IClaimEligibilityEvaluator` in `ResolveOps.Application` / `ResolveOps.Domain`:
     - Evaluates eligibility based on:
       - Compatibility matrix:
         - ExceptionType `Damage` -> ClaimType `CargoDamage`, `TotalLoss`
         - ExceptionType `Shortage` -> ClaimType `Shortage`, `TotalLoss`
         - ExceptionType `Delay` / `InTransitDelay` -> ClaimType `Delay`
         - Incompatible combinations yield `NotEligible` with reason code `INCOMPATIBLE_EXCEPTION_CLAIM_TYPE`.
       - Case resolution and status (case cannot be Closed/Resolved as false alarm).
       - Existing active claim check for same case/carrier.
       - Filing deadline calculation: calculates carrier deadline from incident date/planned delivery date using `IBusinessCalendarService` (carrier contract filing window, e.g. 30, 60, or 90 days; accounts for working hours and holidays per Edge Case 8).
     - Returns: `EligibilityResult(EligibilityStatus Status, string[] ReasonCodes, DateTimeOffset DeadlineAtUtc, Guid PolicyVersionId)`.

3. Claim Readiness & Evidence Checklist Integration (Spec §8.8, §9.2, §16.9, §16.10):
   - Define `IClaimReadinessEvaluator` in `ResolveOps.Application`:
     - Cross-references active, scanned `Available` evidence documents (`evidence_documents`) in the case with `EvidenceRequirement` records (where `claim_type = Claim.ClaimType` and `exception_type = Case.ExceptionType`).
     - Identifies all missing mandatory documents.
     - Returns: `ClaimReadinessResult(bool IsReady, IReadOnlyList<EvidenceRequirementCheckItem> Checklist, string[] MissingRequirements)`.
   - Invariant: A claim CANNOT transition to `ReadyForReview` if mandatory evidence is missing or unscanned/quarantined.

4. Duplicate Active Claim Prevention (Spec §10.5 Invariant 1 & 10):
   - Invariant: Only one active (status != `Cancelled`, `Closed`) claim per `(tenant_id, case_id, carrier_id)`.
   - Enforced both at domain creation and via partial unique index in SQL Server.

5. Claim REST APIs (`ResolveOps.Modules.Claims`) (Spec §16.10):
   - MUST strictly adhere to **Vertical Slice Architecture** with **1 class per file** (Separate files for Endpoint, Handler, Command/Query, Validator, Response):
     - `CreateDraftClaim`: `POST /api/exceptions/{caseId}/claims`
       - Request: `claimType`, `currency`, `policyVersionId` (optional), `expectedCaseVersion`.
       - Validates eligibility, creates claim aggregate, calculates deadline, returns claim response.
     - `GetClaims`: `GET /api/claims`
       - Filter by `caseId`, `carrierId`, `status`, `claimType`, with pagination.
     - `GetClaimById`: `GET /api/claims/{claimId}`
       - Returns claim header, loss components, eligibility details, and readiness status.
     - `AddLossComponent`: `POST /api/claims/{claimId}/loss-components`
       - Adds component, updates claim total, returns component details.
     - `UpdateLossComponent`: `PUT /api/claims/{claimId}/loss-components/{componentId}`
       - Updates existing component, recalculates claim total.
     - `RemoveLossComponent`: `DELETE /api/claims/{claimId}/loss-components/{componentId}`
       - Removes component, recalculates claim total.
     - `CalculateClaimEligibility`: `POST /api/claims/{claimId}/calculate-eligibility`
       - Re-evaluates eligibility against policy and updates claim state.
     - `GetClaimReadiness`: `GET /api/claims/{claimId}/readiness`
       - Evaluates evidence checklist against current available documents for the case.

6. Persistence & EF Core Configuration:
   - EF Core configurations in `src/BuildingBlocks/ResolveOps.Persistence/Configurations/`:
     - `ClaimConfiguration`:
       - Table: `claims`
       - Precision: `claimed_amount`, `approved_amount`, `recovered_amount` as `decimal(19,4)`.
       - Unique index: `(tenant_id, claim_number)`.
       - Partial unique index: `(tenant_id, case_id, carrier_id)` WHERE `status NOT IN ('Cancelled', 'Closed')`.
     - `ClaimLossComponentConfiguration`:
       - Table: `claim_loss_components`
       - Precision: `amount` as `decimal(19,4)`, `quantity` as `decimal(18,3)`, `unit_amount` as `decimal(19,4)`.
   - Update `AppDbContext`: Add `DbSet<Claim> Claims` and `DbSet<ClaimLossComponent> ClaimLossComponents`, configure tenant query filters.
   - EF Core Migration: `AddClaimsAndEligibilityEngine`.

7. Observability & Documentation:
   - OpenTelemetry metrics in `ClaimMetrics.cs`:
     - `claims.drafted.total`, `claims.eligibility.evaluated.total`, `claims.amount.claimed.total`.
   - Create `docs/adr/ADR-027-claim-eligibility-and-draft-claims.md`.
   - Update `AGENTS.md` and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user).
Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 10 from the specification.
3. **Implement only the Phase 10 scope** — do not add features from future phases (e.g., submission approval, carrier response in Phase 11, financial recovery in Phase 12).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Claims`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`).
5. **Enforce 1-class-per-file**: Every vertical slice feature MUST have separate files: `<Action><Resource>Endpoint.cs`, `<Action><Resource>Handler.cs`, `<Action><Resource>Command.cs` / `Query.cs`, `<Action><Resource>Validator.cs`, `<Action><Resource>Response.cs`.
6. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
7. **Ensure ConcurrencyStamp integrity**: Concurrency stamps are managed centrally by `AppDbContext.ApplyAuditAndConcurrency()`; domain methods MUST NOT manually mutate stamps.
8. **Add/update** EF Core migrations, OpenTelemetry instrumentation, and ADR-027.
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

## Definition of Done Checklist (Section 31 & §24 Phase 10)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] All features follow strict 1-class-per-file convention in `ResolveOps.Modules.Claims`
- [ ] `Money` value object implemented with decimal precision and currency validations (no float/double)
- [ ] `Claim` aggregate root and `ClaimLossComponent` entities implemented with guarded business methods
- [ ] Deterministic claim eligibility engine evaluates compatibility between exception types and claim types
- [ ] Claim deadline calculated using `IBusinessCalendarService` and carrier filing window
- [ ] Claim readiness integrates with `evidence_documents` and `evidence_requirements` (Phase 9)
- [ ] Duplicate active claims for the same case/carrier are prevented via domain check and SQL partial index
- [ ] Claimed amount strictly equals the sum of validated loss components
- [ ] REST API endpoints in `ResolveOps.Modules.Claims` implemented with Minimal APIs & FluentValidation
- [ ] EF Core configurations, decimal(19,4) precision, tenant filters, and migration applied
- [ ] Centralized `ConcurrencyStamp` optimistic concurrency preserved
- [ ] OpenTelemetry metrics and activity sources instrumented for claim domain
- [ ] ADR-027 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Ví dụ |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 10 — Claim eligibility and draft claims` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0, 1, 2, 3, 4, 5, 6, 7, 8, 9` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Số migration, số test, file nào đang có |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai Claim aggregate, Money value object, Eligibility engine, Claims API |

### Cách lấy repository state nhanh:

Chạy lệnh này trong repo để lấy thông tin điền vào:
```powershell
# Chạy trong thư mục repo
Write-Host "=== BUILD ===" ; dotnet build --no-restore -q 2>&1 | tail -3
Write-Host "=== ARCH TESTS ===" ; dotnet test tests/ResolveOps.ArchitectureTests/ --no-build -q 2>&1 | tail -5  
Write-Host "=== MIGRATIONS ===" ; dotnet ef migrations list --project src/BuildingBlocks/ResolveOps.Persistence 2>&1
Write-Host "=== GIT STATUS ===" ; git log --oneline -5
```

# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 8.

---

## PROMPT (copy từ đây)

---

You are implementing **ResolveOps**, a Logistics Exception & Carrier Claims management platform.

## Specification

The root specification is:
```
c:\Personal\ResolveOps\LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md
```
*(Hoặc `c:\FSoft\ResolveOps\LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md` tùy môi trường máy).*

Read the specification before making any changes. Pay special attention to:
- Section 0: Agent rules and non-negotiable constraints
- Section 7: Exception taxonomy & severity model
- Section 8.4: Operational exception resolution & mitigation workflow
- Section 8.5: Case comments & chronological timeline audit
- Section 9.1: Exception case states & transition matrix
- Section 9.3: Task state machine (`Open`, `InProgress`, `Completed`, `Blocked`, `Cancelled`)
- Section 9.4: SLA clock states (`NotStarted`, `Running`, `Paused`, `Completed`, `Breached`)
- Section 10.3: Exception case invariants (mandatory incomplete tasks block closure, optimistic concurrency check, owner and severity changes audited)
- Section 12: Architecture and module structure (`ResolveOps.Modules.Workflow` and `ResolveOps.Modules.Exceptions`)
- Section 13: Technology stack
- Section 15.7: Exception tables (`exception_cases`, `case_timeline_entries`)
- Section 15.8: Workflow tables (`workflow_tasks`, `sla_policies`, `sla_policy_versions`, `sla_clocks`, `sla_clock_pauses`)
- Section 16.7: Exception endpoints (`/api/exception-cases/{id}/...` lifecycle commands, comments, timeline)
- Section 16.8: Task endpoints (`/api/tasks/my`, `/api/exception-cases/{caseId}/tasks`, task action endpoints)
- Section 17.3: Outbox integration events (`CaseSlaBreachedV1`, `ExceptionDetectedV1`)
- Section 18.1 & 18.2: Required workers & scheduler (`SlaClockJob` in Quartz.NET for SLA breach monitoring)
- Section 24: Implementation roadmap and Phase 8 definition
- Section 25: Coding standards
- `AGENTS.md` in the repository root

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
| Object Storage | MinIO (Docker: `minio/minio`) — client: `AWSSDK.S3` |
| Cache | Redis 7 (Docker: `redis:7-alpine`) — client: `StackExchange.Redis` |
| Observability | OpenTelemetry + Serilog + Seq (`datalust/seq`) |
| Local Orchestration | .NET Aspire + Docker Compose |
| Frontend | Angular 19+ + TypeScript + Angular Material (Phase 15+) |
| Testing | Architecture tests (`ResolveOps.ArchitectureTests`) |
| Concurrency | Optimistic Concurrency via `string ConcurrencyStamp` (ADR-006) |

**Rejected (do not add):** Azure proprietary services, MassTransit, AutoMapper, generic repositories, React/Vite, microservices, Kubernetes, Kafka.

## Current Implementation State

**Current phase:** `Phase 8 — Exception case workflow, tasks, SLA`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings).
- Architecture tests pass (5 passed, 0 failed).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (`AddHandlersFromAssembly`, `MapEndpoints`).
- DomainErrors unified via `ErrorTemplates` table and `DatabaseErrorMessageProvider` returning RFC 7807 Problem Details.
- All mutable entities use `string ConcurrencyStamp` for optimistic concurrency and implement `IAuditableEntity`.
- Database schema: Migrations through `20260917142552_AddExceptionPoliciesAndCases` are applied.
- Background workers running in `ResolveOps.Worker`:
  - `TrackingIngestionConsumerService` (RabbitMQ v7 async consumer for raw carrier receipts)
  - `ExceptionEvaluationConsumerService` (RabbitMQ v7 async consumer for TrackingEventAcceptedV1)
  - `MissedDeadlineScanJob` (Quartz.NET 1-min periodic scan for missed shipment milestones)
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL test requirements are currently waived.
```

**Stopping point / specific task this session:**
```
Implement Phase 8: Exception case workflow, tasks, and SLA according to Master Spec §24 Phase 8, §8.4–8.5, §9.1, §9.3–9.4, §10.3, §15.8, §16.7, §16.8, §17.3, §18.1–18.2:

1. Exception Case State Machine & Lifecycle (Spec §9.1, §10.3, §16.7):
   - Add transition methods to `ExceptionCase` entity guarding valid transitions:
     - Triage (`Detected` -> `Triaged`)
     - Assign (`Triaged` -> `Assigned`)
     - Start Investigation (`Assigned` -> `Investigating`, or `Reopened` -> `Investigating`)
     - Request Evidence (`Investigating` -> `AwaitingEvidence`)
     - Request Carrier Update (`Investigating` -> `AwaitingCarrier`)
     - Start Mitigation (`Investigating` -> `Mitigating`)
     - Complete Mitigation (`Mitigating` -> `Investigating`)
     - Receive Evidence / Carrier Update (`AwaitingEvidence` / `AwaitingCarrier` -> `Investigating`)
     - Mark Claim Required (`Investigating` -> `ClaimRequired`)
     - Resolve (`Investigating` or `ClaimRequired` -> `Resolved`)
     - Close (`Resolved` -> `Closed`)
     - Reopen (`Closed` -> `Reopened`)
     - Cancel (`Detected` or `Triaged` -> `Cancelled`)
     - Change Severity & Reclassify
     - Add Comment / Timeline entry
   - Invariant §10.3: Prevent direct arbitrary state mutation.
   - Invariant §10.3: `Close` must fail if any mandatory task on the case is still open, in progress, or blocked without an authorized waiver.
   - Optimistic Concurrency: Every command receives `expectedVersion` / `expectedConcurrencyStamp`. If mismatched, return HTTP 409 Conflict with Problem Details.
   - Audit Trail: Every transition and comment appends a `CaseTimelineEntry` recording actor, old state, new state, note, and timestamp.

2. Workflow Tasks Aggregate & Endpoints (Spec §9.3, §15.8, §16.8):
   - Create `WorkflowTask` entity in `ResolveOps.Domain.Workflow`:
     - Fields: `Id`, `TenantId`, `CaseId`, `ClaimId`, `TaskType`, `Title`, `Description`, `Status` (Open, InProgress, Completed, Blocked, Cancelled), `Priority`, `OwnerUserId`, `OwnerTeamCode`, `DueAtUtc`, `BlockedReason`, `CompletionNote`, `CompletedAtUtc`, `IsMandatory`, `WaivedReason`, `WaivedByUserId`, `WaivedAtUtc`, `ConcurrencyStamp`.
   - Implement vertical slices in `ResolveOps.Modules.Workflow`:
     - `GET /api/tasks/my`
     - `GET /api/exception-cases/{caseId}/tasks`
     - `POST /api/exception-cases/{caseId}/tasks` (create manual task)
     - `POST /api/tasks/{taskId}/assign`
     - `POST /api/tasks/{taskId}/start`
     - `POST /api/tasks/{taskId}/block`
     - `POST /api/tasks/{taskId}/unblock`
     - `POST /api/tasks/{taskId}/complete`
     - `POST /api/tasks/{taskId}/cancel`
     - `POST /api/tasks/{taskId}/waive` (requires waiver reason and authorized user)

3. SLA Policies, Clocks & Business Calendar Integration (Spec §9.4, §15.8):
   - Create entities in `ResolveOps.Domain.Workflow`:
     - `SlaPolicy`, `SlaPolicyVersion` (acknowledgement_minutes, first_action_minutes, resolution_minutes, pause_reason_codes)
     - `SlaClock` (clock_type: FirstAction, Resolution; status: Running, Paused, Completed, Breached; target/due_at_utc, elapsed, total_paused_seconds)
     - `SlaClockPause` (sla_clock_id, reason_code, started_at_utc, ended_at_utc)
   - Clock lifecycle triggers:
     - Case created -> Start SLA clocks (`FirstAction`, `Resolution`) with deadlines calculated via `IBusinessCalendarService` using tenant calendar/working hours/holidays.
     - Case triaged -> Complete `FirstAction` clock.
     - Case enters `AwaitingEvidence` or `AwaitingCarrier` -> Pause running clocks if reason code matches allowed pause reasons.
     - Case returns to `Investigating` -> Resume paused clocks and recalculate `DueAtUtc` by adding paused duration through `IBusinessCalendarService`.
     - Case resolved -> Complete `Resolution` clock.

4. SLA Breach Monitoring Job (Spec §18.1, §18.2, §17.3):
   - Implement `SlaBreachScanJob` (Quartz.NET job in `ResolveOps.Worker`) running periodically (e.g. every 1 minute).
   - Queries `sla_clocks` where `status = 'Running'` and `due_at_utc <= TimeProvider.GetUtcNow()`.
   - Transitions clock to `Breached`, appends timeline entry to the case, and publishes `CaseSlaBreachedV1` integration event to the Outbox.

5. Database Mapping & Migrations (Spec §15.8):
   - Add EF Core entity configurations in `ResolveOps.Persistence`:
     - `WorkflowTaskConfiguration`
     - `SlaPolicyConfiguration` & `SlaPolicyVersionConfiguration`
     - `SlaClockConfiguration` & `SlaClockPauseConfiguration`
   - Include tenant query filters and composite/partial indexes:
     - Index on `sla_clocks(tenant_id, status, due_at_utc)`
     - Index on `workflow_tasks(tenant_id, case_id, status)`
     - Index on `workflow_tasks(tenant_id, owner_user_id, status)`
   - Add EF Core Migration: `AddWorkflowTasksAndSlaClocks`.

6. Observability & Architecture Documentation:
   - Add OpenTelemetry metrics in `WorkflowMetrics.cs` (`workflow.tasks.completed`, `workflow.sla.breached`, `workflow.case.transitions`).
   - Create `docs/adr/ADR-025-exception-case-workflow-and-sla.md`.
   - Update `AGENTS.md` (mark Phase 8 complete, set next phase to Phase 9) and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user).
Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 8 from the specification.
3. **Implement only the Phase 8 scope** — do not add features from future phases (e.g., MinIO evidence upload in Phase 9, Claim draft in Phase 10).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Workflow` and `ResolveOps.Modules.Exceptions`).
5. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
6. **Add/update** EF Core migrations, OpenTelemetry instrumentation, and ADR-025.
7. **Run** formatting (`dotnet format`), build (`dotnet build ResolveOps.slnx -c Release`), and architecture tests (`dotnet test tests/ResolveOps.ArchitectureTests/ -c Release`).
8. **Fix** any failures caused by your changes before reporting done.
9. **Update** `CHANGELOG.md` and `AGENTS.md` with current phase status.
10. **Report** at the end: files changed, commands run, test results, assumptions made, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT use AutoMapper — mapping must be explicit.
- Do NOT put business logic in endpoints.
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user).

## Definition of Done Checklist (Section 31 & §24 Phase 8)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures
- [ ] Arbitrary status assignment is impossible; every state change is an audited command
- [ ] Incomplete mandatory tasks block case closure unless waived with authorized reason
- [ ] Stale concurrent mutations return HTTP 409 Conflict with Problem Details
- [ ] SLA clocks start, pause, resume, and complete according to policy and business calendar
- [ ] Quartz.NET SLA breach scanner marks overdue clocks as breached and emits `CaseSlaBreachedV1` to outbox
- [ ] Case timeline records all transitions, assignments, severity changes, and comments with actor attribution
- [ ] EF Core migration applied for `workflow_tasks`, `sla_policies`, `sla_policy_versions`, `sla_clocks`, and `sla_clock_pauses`
- [ ] Tenant-aware query filters applied to all new entities
- [ ] OpenTelemetry metrics and ActivitySource instrumented for workflow and SLA
- [ ] ADR-025 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Ví dụ |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 8 — Exception case workflow, tasks, SLA` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0, 1, 2, 3, 4, 5, 6, 7` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Số migration, số test, file nào đang có |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai workflow task, SLA clock, Quartz job |

### Cách lấy repository state nhanh:

Chạy lệnh này trong repo để lấy thông tin điền vào:
```powershell
# Chạy trong thư mục repo
Write-Host "=== BUILD ===" ; dotnet build --no-restore -q 2>&1 | tail -3
Write-Host "=== ARCH TESTS ===" ; dotnet test tests/ResolveOps.ArchitectureTests/ --no-build -q 2>&1 | tail -5  
Write-Host "=== MIGRATIONS ===" ; dotnet ef migrations list --project src/BuildingBlocks/ResolveOps.Persistence 2>&1
Write-Host "=== GIT STATUS ===" ; git log --oneline -5
```

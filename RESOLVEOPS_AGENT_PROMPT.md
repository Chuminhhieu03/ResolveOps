# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 15.

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
- Section 10.6: Tenant invariants (tenant isolation, tenant switching, tenant header/context on all API calls)
- Section 11 & Section 19.9: Edge cases & Security:
  - Stale concurrency conflict handling: ConcurrencyStamp conflict (HTTP 409) must present user-friendly conflict resolution dialogs without silent data loss.
  - Sensitive financial fields hidden where unauthorized (Finance & Claims Specialist vs Logistics Coordinator).
  - Presigned URL integration: Evidence document uploads via upload-intent to MinIO, secure downloads via presigned URLs.
- Section 13.4: Frontend architecture & technology stack:
  - Angular 19+ SPA with TypeScript in `web/resolveops-web`
  - Angular Material (`@angular/material`) for accessible enterprise UI components
  - Angular HttpClient + RxJS for reactive server-state management and HTTP interceptors (JWT auth, error normalization, correlation ID)
  - Angular Reactive Forms + Zod for client-side schema validation
  - `@microsoft/signalr` wrapped in RxJS observable service for real-time notification streams
  - Playwright for end-to-end browser journeys
- Section 16: Complete API endpoints inventory:
  - Authentication & Identity: `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/tenants`, `GET /api/users/me`
  - Partners & Locations: `GET /api/carriers`, `GET /api/locations`, `GET /api/calendars`
  - Shipments: `GET /api/shipments`, `GET /api/shipments/{id}`
  - Tracking Ingestion & Quarantine: `GET /api/tracking/events`, `GET /api/tracking/quarantine`, `POST /api/tracking/quarantine/{id}/replay`
  - Exception Cases: `GET /api/exceptions/cases`, `GET /api/exceptions/cases/{id}`, `POST /api/exceptions/cases/{id}/triage`, `POST /api/exceptions/cases/{id}/assign`, `POST /api/exceptions/cases/{id}/resolve`, `GET /api/exceptions/cases/{id}/tasks`, `POST /api/exceptions/cases/{id}/tasks/{taskId}/complete`
  - Evidence & Documents: `GET /api/evidence/cases/{caseId}/documents`, `POST /api/evidence/upload-intent`, `GET /api/evidence/documents/{documentId}/download`
  - Claims: `GET /api/claims`, `GET /api/claims/{id}`, `POST /api/claims/prepare`, `POST /api/claims/{id}/components`, `POST /api/claims/{id}/approve`, `POST /api/claims/{id}/submit`, `POST /api/claims/{id}/decision`, `POST /api/claims/{id}/recoveries`, `POST /api/claims/{id}/write-offs`
  - Notifications & Realtime: `GET /api/notifications`, `GET /api/notifications/unread-count`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`, `GET/PUT /api/notifications/preferences`, SignalR Hub `/hubs/notifications`
  - Dashboards & Reports: `GET /api/dashboard/operations`, `GET /api/dashboard/claims`, `GET /api/reports/exception-ageing`, `GET /api/reports/sla-performance`, `GET /api/reports/carrier-scorecards`, `GET /api/reports/financial-recovery`, `POST /api/exports/exception-cases`, `GET /api/exports/{exportId}`
  - Policies: `GET /api/policies/sla`, `GET /api/policies/eligibility`, `GET /api/policies/evidence`
- Section 24: Phase 15 definition, 12 required enterprise screens, UX requirements, and Definition of Done
- ADR-021: Angular for Frontend
- ADR-006: Auto-discovery and Concurrency Stamp

## Technology Stack (mandatory — do not substitute)

| Layer | Technology |
|---|---|
| Frontend Framework | Angular 19+ (Standalone Components, TypeScript 5+) |
| UI Component Library | Angular Material (`@angular/material`) |
| Reactive Programming | RxJS (Observables, Subjects, BehaviorSubjects) |
| Client Validation | Zod + Angular Reactive Forms |
| Realtime WebSockets | `@microsoft/signalr` (wrapped in RxJS service) connected to `/hubs/notifications` |
| HTTP & API Client | Angular `HttpClient` with functional Interceptors (Auth, Correlation ID, Problem Details) |
| Runtime & Language | Node >= 22.0.0 (per `.nvmrc`), TypeScript, SCSS/CSS |
| Backend Runtime | .NET 10 LTS, ASP.NET Core Minimal APIs, EF Core 10, SQL Server 2022 |
| Messaging & Storage | RabbitMQ 3.x, MinIO S3 Object Storage, Redis 7 |
| Testing | Architecture tests (`ResolveOps.ArchitectureTests`), Playwright E2E |

**Rejected (do not add):** React, Vite, Vue, Blazor, microservices, Kubernetes, Kafka, generic repositories.

## Current Implementation State

**Current phase:** `Phase 15 — Frontend production workflow`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA), Phase 9 (evidence and secure document pipeline), Phase 10 (claim eligibility and draft claims), Phase 11 (claim approval, submission, response, appeal), Phase 12 (financial recovery and settlement), Phase 13 (notifications and realtime operations), Phase 14 (reporting and carrier scorecards)`

**Repository state summary:**
```
- Backend builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings across all 29 .NET projects).
- Architecture tests pass (5 passed, 0 failed).
- Code formatting passes strict verification (dotnet format ResolveOps.slnx --verify-no-changes exits with code 0).
- Modular Monolith API with Minimal APIs and Vertical Slice Architecture exposing all required endpoints and SignalR notification hub (/hubs/notifications).
- Database migrations fully applied through 20260923151333_AddReportingAndCarrierScorecards.
- Background workers and Quartz.NET scheduled jobs active in ResolveOps.Worker:
  - TrackingIngestionConsumerService
  - ExceptionEvaluationConsumerService
  - NotificationConsumerService
  - ReportingProjectionConsumerService
  - MissedDeadlineScanJob
  - SlaBreachScanJob
  - ClaimFollowUpScanJob
  - NotificationEmailRetryScanJob
  - DocumentProcessingWorker
  - AbandonedUploadCleanupJob
  - ExportProcessingJob
- Frontend stub located in web/resolveops-web (contains .nvmrc and package.json).
- Node version: Node v22.12.0, npm 11.6.2.
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL UT/IT test requirements are currently waived. Backend architecture tests and clean frontend compilation (0 errors) remain strictly mandatory.
```

**Stopping point / specific task this session:**
```
Implement Phase 15: Frontend production workflow according to Master Spec §24 Phase 15, §0, §4.1, §4.2, §10.6, §11, §13.4, §16, §19.9, §25, and ADR-021:

1. Frontend Architecture & Scaffolding (in web/resolveops-web):
   - Configure Angular 19+ standalone application with Angular Material, TypeScript, and RxJS.
   - Configure angular.json, tsconfig.json, proxy.conf.json (proxying /api and /hubs to backend host).
   - Enterprise layout with responsive top navigation header (brand, active tenant switcher, real-time notification bell with unread badge, user profile menu with role badge, logout) and collapsible sidebar navigation.
   - Core Services & Interceptors:
     - AuthService: login, refresh token rotation, logout, current user & role signal/observable.
     - TenantService: active tenant resolution, tenant switcher, sets X-Tenant-Id header.
     - AuthInterceptor: attaches JWT Bearer token to API requests; injects X-Correlation-Id header.
     - ApiErrorInterceptor: transforms RFC 7807 Problem Details into user-friendly notifications; detects 409 ConcurrencyStamp conflicts.
     - SignalRNotificationService: connects to /hubs/notifications with JWT token, reconnects on failure, streams real-time notifications to toast/snackbar and bell badge.
   - Route Guards: AuthGuard, TenantGuard, RoleGuard for permission-aware navigation.

2. Twelve Required Enterprise Screens (Spec §24 Phase 15):
   1. Login Screen:
      - Clean enterprise auth card with email/password, demo user quick-fill presets (Operations Manager, Claims Specialist, Logistics Coordinator, Finance), tenant selector, error display.
   2. Operational Dashboard (/dashboard/operations):
      - Real-time KPI summary cards: Open Exceptions by severity (Critical, High, Medium, Low), SLA Breach & At-Risk count (nearing breach within 2 hours), Unassigned Tasks, Active Carrier Delay Rate, Ingestion Throughput.
      - Severity distribution visual chart, SLA status progress bars, quick-action navigation.
   3. Exception Work Queue (/exceptions):
      - High-performance data table with server-side pagination, sorting, search.
      - Filters: Carrier, Severity (Critical, High, Medium, Low), Status, Date Range.
      - Quick actions: Triage modal (severity adjustment, root cause tag), Assignee drawer.
      - Prominent SLA countdown badge (color-coded: green = healthy, amber = at risk, red = breached).
   4. Exception Case Detail with Timeline (/exceptions/{id}):
      - Header: Tracking number, carrier, priority chip, SLA clock countdown, current state.
      - Interactive chronological audit timeline (tracking milestones, detected events, notes).
      - Action controls: Assign, Triage, Resolve, Create Claim (guarded by role and state).
      - Concurrency conflict handling: Gracefully alerts user if ConcurrencyStamp changed, offering 1-click reload.
   5. Shipment Detail and Milestones (/shipments/{id}):
      - Origin, destination, service level, expected vs actual delivery dates.
      - Visual milestone progression tracker (Accepted, In Transit, Out for Delivery, Delivered, Exception).
      - Raw tracking event history with timestamps and checkpoint codes.
   6. Task List (/tasks):
      - Operational task queue with priority indicators, due dates, assignee filters.
      - 1-click task status update (Pending -> InProgress -> Completed) with comment.
   7. Evidence Checklist & Upload Pipeline (/exceptions/{id}/evidence):
      - Checklist of mandatory vs optional evidence documents per exception type.
      - Direct MinIO upload via presigned URL: calls POST /api/evidence/upload-intent, streams file directly to S3 storage, confirms upload to API.
      - Virus scan status indicator (Pending, Clean, Infected) and secure document viewer / presigned download link.
   8. Claim Preparation & Amount Components (/claims/prepare or /claims/{id}/edit):
      - Draft claim wizard: selects eligible exception case and carrier.
      - Loss components dynamic table: Item cost, freight charge, labor, customs, salvage credit.
      - Real-time auto-calculation of Total Claimed Amount.
      - Client-side validation via Zod schemas ensuring non-negative decimals and valid currencies.
      - Rule 4 Invariant: Explicitly show that financial transitions require human operator approval.
   9. Claim Approval, Submission & Decision Timeline (/claims/{id}):
      - Tier-based approval workflow UI: Approve for submission button enabled only for authorized roles (Logistics Coordinator vs Claims Specialist vs Operations Manager based on amount thresholds).
      - Carrier submission package preview (manifest, claim letter, evidence links).
      - Carrier response modal: Record Approval, Rejection (with reason code), or Dispute.
      - Financial settlement modal: Record recovery payment / credit note / write-off with amounts.
   10. Carrier Scorecard (/reports/carrier-scorecards):
       - Date range and carrier filter selectors.
       - 7-metric scorecard table: Total Shipments, Exception Rate %, On-Time Rate %, Severity Distribution, Average Response Time (hours), Claim Approval Rate %, Recovery Rate %.
   11. Integration Quarantine Screen (/quarantine):
       - List of failed/quarantined tracking receipts and malformed payloads.
       - Formatted JSON payload viewer with syntax highlighting and failure reason.
       - Replay button (POST /api/tracking/quarantine/{id}/replay) with success feedback.
   12. Tenant Policy Administration (/admin/policies):
       - SLA policy viewer & editor: acknowledgement minutes, resolution minutes, pause codes.
       - Claim eligibility rules configuration and evidence requirement checklist per exception type.

3. UX Polish, State & Concurrency Handling:
   - Clear loading skeletons, spinners, and empty state illustrations across all tables and cards.
   - Stale Concurrency Conflict Handling: Displays non-destructive conflict modal when receiving HTTP 409 with "Reload Latest Data" action.
   - Timezone formatting: All UTC timestamps from API rendered in tenant/user local timezone using custom pipes.
   - Realtime SignalR notification toasts (Angular Material MatSnackBar) and notification drawer slide-out.
   - Formula-safe rendering: Table cells beginning with '=', '+', '-', '@' escaped.

4. Architecture & Documentation:
   - Create `docs/adr/ADR-032-frontend-production-workflow.md`.
   - Update `AGENTS.md` (Current Phase: Phase 15 Complete, Next: Phase 16) and `CHANGELOG.md`.

Do NOT write Unit Tests or Integration Tests (waived by user). Frontend compilation (npm run build with 0 errors) and backend architecture tests remain strictly mandatory.
```

## Your Task

1. **Inspect** the existing repository and frontend stub in `web/resolveops-web`.
2. **Scaffold and build** the Angular 19+ application in `web/resolveops-web` with Angular Material, TypeScript, and RxJS.
3. **Implement all 12 required screens** and shared UI components adhering strictly to enterprise UX requirements.
4. **Implement core infrastructure**: Auth interceptor (JWT Bearer), Tenant switcher, SignalR notification service (`/hubs/notifications`), ApiClient with RFC 7807 Problem Details handling.
5. **Enforce Optimistic Concurrency**: Ensure all update requests pass `concurrencyStamp` and handle HTTP 409 Conflict with clear user prompts.
6. **Enforce Role-based UI visibility**: Hide or disable financial transitions and policy admin for unauthorized personas per Section 4.1/4.2.
7. **Ensure clean compilation**: Run `npm run build` in `web/resolveops-web` (must succeed with 0 errors).
8. **Verify backend integrity**: Run `dotnet build ResolveOps.slnx -c Release` and `dotnet test tests/ResolveOps.ArchitectureTests/ -c Release` (both must pass with 0 warnings/errors).
9. **Write** `docs/adr/ADR-032-frontend-production-workflow.md` documenting frontend architecture, state management, and real-time integration.
10. **Update** `CHANGELOG.md` and `AGENTS.md` with Phase 15 completion status.
11. **Report** at the end: files created/modified, commands run, build results, and architectural summary.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT allow an LLM or AI to approve, reject, pay, or execute financial transitions on claims (Rule 4 & Invariant 12).
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT hardcode secrets, tokens, or private credentials in frontend code.
- Do NOT duplicate authoritative business rules on the frontend; server validation remains authoritative.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests and build verification with 0 warnings/errors remain mandatory.

## Definition of Done Checklist (Section 31 & §24 Phase 15)

Before marking the phase complete, verify:
- [ ] Angular 19+ SPA in `web/resolveops-web` compiles cleanly (`npm run build` exits with code 0)
- [ ] Backend solution builds cleanly in Release mode (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] All 12 required screens implemented:
  - [ ] 1. Login screen with demo credentials and tenant selector
  - [ ] 2. Operational dashboard with real-time KPI metrics, SLA at-risk indicators, and severity distribution
  - [ ] 3. Exception work queue with sorting, filtering, pagination, and quick triage
  - [ ] 4. Exception case detail with interactive chronological audit timeline and state action buttons
  - [ ] 5. Shipment detail with visual milestone progression tracker and raw tracking history
  - [ ] 6. Task list with priority indicators and 1-click status updates
  - [ ] 7. Evidence checklist with direct MinIO presigned URL upload pipeline and virus scan status
  - [ ] 8. Claim preparation wizard with dynamic loss components table and real-time total calculations
  - [ ] 9. Claim approval, submission, carrier response, and financial recovery workflow
  - [ ] 10. Carrier scorecard report with all 7 spec metrics
  - [ ] 11. Integration quarantine screen with raw JSON payload viewer and replay action
  - [ ] 12. Tenant policy administration for SLA policies and claim eligibility rules
- [ ] Real-time SignalR notification service integrated with `/hubs/notifications`, notification drawer, unread counter, and toast alerts
- [ ] HTTP interceptors configured for JWT Bearer tokens, correlation IDs, and Problem Details error handling
- [ ] Stale concurrency conflict (HTTP 409) handled gracefully with user reload dialog
- [ ] Tenant and role restrictions reflected in UI controls and navigation guards
- [ ] UTC timestamps formatted to tenant/user local timezone
- [ ] Sensitive financial actions and fields hidden where unauthorized
- [ ] ADR-032 written in `docs/adr/ADR-032-frontend-production-workflow.md`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Giá trị cho Phase 15 |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 15 — Frontend production workflow` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0 through Phase 14 (Reporting and carrier scorecards)` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Clean build Release 0 errors/warnings (29 projects), ArchTests 5/5 pass, EF migration `20260923151333_AddReportingAndCarrierScorecards`, all backend endpoints & SignalR hubs ready |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai Frontend Angular 19+ SPA trong `web/resolveops-web`, thiết lập Angular Material, layout doanh nghiệp, HTTP interceptors, SignalR real-time client, 12 màn hình nghiệp vụ (Login, Dashboards, Exception Queue, Case Detail timeline, Shipment milestones, Task list, Evidence upload intent, Claim wizard, Approval & Settlement, Carrier scorecards, Quarantine replay, Policy admin), xử lý concurrency conflict (409) và role-based authorization |


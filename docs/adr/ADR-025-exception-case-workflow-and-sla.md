# ADR-025 — Exception Case Workflow, Task State Machine, Business Calendar, and SLA Tracking

- **Date:** 2026-09-17
- **Status:** Accepted
- **Phase:** Phase 8 — Exception case workflow, tasks, SLA

---

## Context

In freight logistics operations, resolving transit exceptions and damages requires multi-party collaboration across dispatchers, claims coordinators, carriers, and shippers:
1. **Guarded Lifecycle & Invariants:** Ad-hoc or direct status mutations can lead to invalid states (e.g., skipping investigation, closing cases with unfulfilled operational duties, or resolving without recorded mitigation).
2. **Mandatory Task Blocking (Master Spec §10.3):** Operational workflows define mandatory tasks (e.g., inspect cargo, collect bill of lading, file preliminary carrier notice). A case must never be closed while mandatory tasks remain incomplete, unless an authorized operator explicitly waives them with an audited justification.
3. **Business Hours and Working Calendar:** Logistics contracts specify SLAs in business hours (e.g. 8 business hours to triage, 48 business hours to resolve), excluding weekends, holidays, and non-working hours. Wall-clock calculations produce false breaches during weekends or bank holidays.
4. **SLA Clock Pausing:** When waiting for third-party input (carrier response, customer evidence), SLA clocks must pause. Upon receiving the requested input, the clock resumes with the target deadline extended by the accumulated pause duration.
5. **Background Breach Detection:** SLA breaches must be detected reliably and promptly without relying on user queries, publishing outbox events (`CaseSlaBreachedV1`) to trigger alerts and escalation workflows.
6. **Optimistic Concurrency:** Concurrent operator edits on cases and tasks must be detected via `ConcurrencyStamp` (ADR-006) returning HTTP 409 Conflict rather than silently overwriting state.

---

## Decision

We implement a modular, event-driven workflow engine, task state machine, and SLA tracking subsystem:

### 1. Guarded Case Lifecycle State Machine (§9.1)
- State transitions on `ExceptionCase` (`Triage`, `Assign`, `StartInvestigation`, `RequestEvidence`, `ReceiveEvidence`, `RecordCarrierUpdate`, `StartMitigation`, `CompleteMitigation`, `MarkClaimRequired`, `Resolve`, `Close`, `Reopen`, `ChangeSeverity`, `Reclassify`) are encapsulated domain methods.
- Transitions strictly adhere to the spec transition matrix. Direct assignment of status is disallowed.
- Every state change appends a timestamped `CaseTimelineEntry` with actor identity, old/new states, metadata, and reason.

### 2. Workflow Tasks and Mandatory Task Invariant (§9.3, §10.3)
- `WorkflowTask` entities model required operational steps with states `Open`, `InProgress`, `Completed`, `Blocked`, and `Cancelled`.
- Tasks support assignment, priority, due dates, blocking with reasons, completion, and waiving.
- Invariant §10.3 is enforced at the domain aggregate level in `ExceptionCase.Close()`:
  - If any mandatory task (`IsMandatory == true`) is not completed (`Status != WorkflowTaskStatus.Completed`) and not waived (`IsWaived == false`), the closure attempt throws an `InvalidOperationException` and returns a domain validation failure.

### 3. Business Calendar Working-Hours Deadline Calculation (§16.7)
- `IBusinessCalendarService` / `BusinessCalendarService` computes working deadlines by traversing active working intervals defined by tenant operating schedules, partner calendars, and holiday lists (`BusinessCalendar`, `Holiday`, `WorkingHours`).
- If no tenant calendar is configured, it falls back to standard business hours (Monday–Friday 08:00–17:00 UTC).

### 4. SLA Clock Lifecycle and Pause Engine (§9.4)
- SLA policies (`SlaPolicy`, `SlaPolicyVersion`) define response and resolution SLA targets per priority, exception type, or severity.
- `ISlaClockService` / `SlaClockService` manages clocks for `Response` and `Resolution`:
  - `InitializeClocksAsync`: Starts initial SLA clocks using business calendar targets when a case is created.
  - `PauseClockAsync`: Transitions status to `Paused`, logging a `SlaClockPause` interval with reason.
  - `ResumeClockAsync`: Closes the active pause interval, recalculates target deadlines shifted by pause duration via the business calendar, and returns the clock to `Running`.
  - `CompleteClockAsync`: Marks the clock as `Completed` upon milestone fulfillment (first response or resolution).

### 5. Scheduled SLA Breach Scanner Job (§18.1)
- `SlaBreachScanJob` runs every minute via Quartz.NET across all active tenants.
- Queries all `Running` SLA clocks where `TargetDeadlineUtc <= now`.
- Marks the clock as `Breached`, records `BreachedAtUtc`, transitions the case if appropriate, and appends a `CaseSlaBreachedV1` integration event atomically to the Outbox.

### 6. Optimistic Concurrency and Tenant Isolation
- `ExceptionCase`, `WorkflowTask`, and `SlaClock` implement `IHasConcurrencyStamp` for optimistic concurrency verification.
- Concurrency token validation and automatic stamp generation are managed centrally and exclusively by `AppDbContext.ApplyAuditAndConcurrency()` on `SaveChangesAsync()`; domain methods do not manually assign stamps.
- EF Core global query filters enforce `tenant_id` isolation across all task, policy, clock, and pause queries.

---

## Consequences

- Full compliance with Master Spec §8.4, §8.5, §9.1, §9.3, §9.4, §10.3, and Phase 8 deliverables.
- Complete chronological audit trail for all operational decisions and evidence handoffs.
- No possibility of silently dropping tasks or closing cases with unfulfilled mandatory obligations.
- Real-time SLA visibility and automated breach escalation without race conditions.
- Metrics and traces emitted through OpenTelemetry (`sla.breaches.total`, `sla.evaluations.total`, `tasks.created.total`, `tasks.completed.total`).

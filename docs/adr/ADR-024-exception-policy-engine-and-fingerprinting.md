# ADR-024 — Exception Policy Engine, Deterministic Fingerprinting, and Race Safety

- **Date:** 2026-09-17
- **Status:** Accepted
- **Phase:** Phase 7 — Exception policy engine and case creation

---

## Context

In high-volume logistics networks, delay conditions and damage signals arrive concurrently via real-time carrier tracking events and recurring scheduled deadline scans:
1. Webhooks and scanner jobs may detect the same exception condition simultaneously.
2. Repeated scans must never duplicate an active case for the same shipment and delay window.
3. Multiple carrier tracking events reporting damage or transit disruption on the same leg should append operational occurrences rather than spawning separate cases.
4. Changes to exception or SLA policies must never retroactively alter the history or evaluation inputs of existing cases.
5. Operators must be able to cancel false positives with mandatory reasons without breaking concurrency invariants.

## Decision

We implement a deterministic exception policy engine with filtered unique indexing and outbox-driven event notification:

1. **Versioned Immutable Exception Policies:**
   - Policies (`exception_policies`) are versioned with unique constraint `(tenant_id, policy_key, version_number)`.
   - Activating a policy version retires previous versions for that policy key.
   - Rule, severity, and assignment definitions are controlled JSON schemas, not arbitrary executable code.
   - Each created `ExceptionCase` permanently records its `policy_id` and `policy_version_number`.

2. **Deterministic Exception Fingerprinting:**
   - Fingerprints are generated deterministically:
     `TenantId + ":" + ShipmentId + ":" + ShipmentLegId + ":" + ExceptionType + ":" + BusinessKey + ":v" + PolicyVersion`
   - For `PickupDelay`: business key is planned pickup time ISO string.
   - For `InTransitDelay`: business key is planned delivery time ISO string.
   - For `Damage`: business key is the carrier external tracking event ID or tracking event ID.

3. **Filtered Unique Index (Database-Level Race Guard):**
   - We enforce case uniqueness using SQL Server partial unique index:
     `UNIQUE INDEX UIX_ExceptionCases_ActiveFingerprint (tenant_id, fingerprint) WHERE status NOT IN ('Closed', 'Cancelled')`
   - This guarantees that repeated deadline scans or concurrent event evaluations cannot create duplicate active cases even under high concurrency.
   - Concurrency conflicts (`DbUpdateException`) on insertion are caught gracefully, falling back to attaching subsequent signals as `ExceptionOccurrence` records.

4. **Dual Trigger Model (Event-Driven & Scheduled Scan):**
   - **Event-Driven:** `ExceptionEvaluationConsumerService` in `ResolveOps.Worker` consumes `TrackingEventAcceptedV1` from RabbitMQ using the transactional Inbox pattern, evaluating incoming events for `Damage` and `InTransitDelay`.
   - **Scheduled Deadline Scan:** `MissedDeadlineScanJob` running via Quartz.NET periodically evaluates active shipments against planned pickup and delivery deadlines plus tolerance windows.

5. **Atomic Outbox Integration Event:**
   - Whenever an `ExceptionCase` is created, `ExceptionDetectedV1` is written atomically to the outbox in the same database transaction, decoupling downstream SLA clock initialization, task generation, and notifications.

6. **False-Positive Cancellation & Concurrency:**
   - Cases in `Detected` or `Triaged` states can be cancelled via `POST /api/exception-cases/{id}/cancel`.
   - Cancellations require a mandatory reason and check `ConcurrencyStamp` for optimistic concurrency (ADR-006), appending a `Cancelled` timeline entry with actor attribution.

## Consequences

- Exactly one active case exists per underlying exception condition.
- Repeated scans and event replays are fully idempotent.
- Case creation and downstream event publishing are strictly consistent (no phantom cases or lost events).
- Operational and historical auditability is preserved across policy revisions.
- OpenTelemetry metrics (`exceptions.detected.total`, `exceptions.detection.duration.ms`) and activity traces measure evaluation throughput and latency.

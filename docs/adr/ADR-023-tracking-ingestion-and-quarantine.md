# ADR-023 — Tracking Ingestion, Idempotent Normalization, and Quarantine Pipeline

- **Date:** 2026-09-11
- **Status:** Accepted
- **Phase:** Phase 6 — Tracking ingestion and normalization

---

## Context

Tracking events from external logistics carriers are inherently unreliable:
1. Carriers re-deliver webhooks on network blips or timeouts, leading to duplicate ingestion attempts.
2. Webhook requests originate outside the trust boundary and require cryptographic authenticity verification without exposing secrets.
3. Tracking events can arrive out of order (e.g. an "OutForDelivery" event arriving after a "Delivered" event).
4. Tracking numbers may refer to shipments that have not yet been registered in the system (or tracking aliases that are missing).
5. Synchronously executing full exception detection and notification pipelines inside carrier webhook requests would risk HTTP timeouts and carrier retry storms.

## Decision

We implement a decoupled, reliable tracking ingestion and normalization pipeline:

1. **Webhook HMAC Authentication:**
   - External webhooks validate HMAC-SHA256 signatures passed via the `X-Signature` header using constant-time comparison (`CryptographicOperations.FixedTimeEquals`).
   - Webhooks with invalid signatures are rejected immediately with `401 Unauthorized`.

2. **Duplicate Replay Guard:**
   - A filtered unique index `(tenant_id, source_system, external_event_id)` on `inbound_event_receipts` prevents duplicate processing.
   - Repeated webhook deliveries return `202 Accepted` / `200 OK` immediately with the existing receipt ID without publishing duplicate normalization work.

3. **Durable Inbound Receipts & Outbox Decoupling:**
   - Inbound event payloads are stored durably in `inbound_event_receipts` before any asynchronous side-effects begin.
   - In the same database transaction, a `TrackingIngestionRequestedV1` integration event is written to `outbox_messages`.
   - The webhook endpoint returns `202 Accepted` rapidly without waiting for normalization or exception workflows.

4. **Transactional Inbox Normalization Consumer:**
   - The Worker host runs `TrackingIngestionConsumerService`, which consumes from the durable `resolveops.tracking-ingestion` RabbitMQ queue.
   - Idempotency is enforced via the `inbox_messages` table in SQL Server.

5. **Shipment Matching & Quarantine:**
   - Normalization resolves the shipment by querying `ShipmentTrackingAlias` (`CarrierId` + `TrackingNumber`).
   - If not matched, it falls back to `Shipment.ExternalReference`.
   - If still unmatched or if the payload is malformed, the event is saved to `quarantined_events` with an appropriate reason code (`UNMATCHED_SHIPMENT`, `INVALID_PAYLOAD`), and the receipt is marked `Quarantined`.
   - Operational endpoints (`GET /api/integration-operations/quarantined-events`, `POST .../resolve`, `POST .../reprocess`) allow operators to manually resolve or reprocess quarantined events.

6. **Immutable Canonical Tracking Events & Out-of-Order Safety:**
   - Matched events are recorded as insert-only rows in `tracking_events`.
   - The `Shipment` aggregate applies the event via `ApplyTrackingEvent`, updating actual milestones (`ActualPickupAtUtc`, `ActualDeliveryAtUtc`).
   - Invariant: A shipment with status `Delivered` will never regress back to `InTransit` when a late out-of-order event arrives.

7. **Downstream Event Publication:**
   - Successfully normalized events emit `TrackingEventAcceptedV1` to the outbox, providing the input signal for Phase 7 Exception policy evaluation.

## Consequences

- Webhook endpoints respond with minimal latency and high availability.
- Carrier webhook retries do not create duplicate business effects or duplicate database entities.
- Unmatched events are visible and recoverable rather than silently dropped.
- Out-of-order events do not corrupt shipment lifecycle status.
- End-to-end tracing and OpenTelemetry metrics (`tracking.receipts.total`, `tracking.normalized.total`, `tracking.quarantined.total`, `tracking.normalization.duration.ms`) provide operational visibility.

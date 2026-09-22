# ADR-030 — Notifications and Realtime Operations Architecture

**Status:** Accepted  
**Date:** 2026-09-22  
**Phase:** 13  
**Authors:** Coding Agent  

---

## Context

Phase 13 establishes the notifications and realtime communication subsystem for ResolveOps (spec §0 Rule 10, §4.1/4.2, §8.8–8.11, §10.6, §11 Edge Cases 13, 14, §12, §15, §16, §17.3/17.4, §18.1, §24 Phase 13, §25).

Operational workflows require immediate, multi-channel alerting across distinct personas (Logistics Coordinators, Exception Specialists, Claims Specialists, Operations Managers, and Finance) when critical events occur:
- SLA clock breaches (`CaseSlaBreachedV1`)
- Exception escalations (`ExceptionDetectedV1`)
- Carrier claim decisions (`ClaimDecisionRecordedV1`)
- Financial recoveries (`ClaimRecoveryRecordedV1`)
- Claim review and submission milestones (`ClaimSubmittedV1`)

Key constraints and architectural requirements:
1. **Rule 10 (Transactional Boundaries)**: Database transactions must NEVER be held open across slow external HTTP calls, message broker deliveries, or external SMTP email dispatches.
2. **Edge Cases 13 & 14 (Mid-Batch Worker Crash & Redelivery Idempotency)**: When the background notification worker crashes or redelivers a batch, duplicate emails and in-app notifications must be strictly prevented.
3. **Mandatory Notification Classes**: Certain critical alerts (`SlaBreachAlert`, `ExceptionEscalation`, `ClaimReviewRequested`) cannot be disabled by users in their notification preferences.
4. **Multi-Tenant Realtime Broadcasting**: WebSocket/SignalR connections must be authenticated (including query-string JWT token extraction during WebSocket handshake) and partitioned into isolated tenant and user groups.

---

## Decision

### 1. Delivery Data Model & Idempotency Guarantee
- Notifications are modeled with four entities in `ResolveOps.Domain.Notifications`:
  - `Notification`: In-app notification record with user assignment, read state, and metadata payload.
  - `NotificationPreference`: Per-user channel preferences for each notification class. Mandatory classes are guarded in domain logic against deactivation.
  - `NotificationTemplate`: Templated notification content supporting localized placeholders.
  - `NotificationDelivery`: Delivery execution tracker with a unique database constraint on `(tenant_id, idempotency_key)` (`uix_notification_deliveries_tenant_idempotency`).
- Idempotency keys follow structured prefixes:
  - In-App: `{NotificationClass}:inapp:{CorrelationId}:{UserId}`
  - Email: `{NotificationClass}:email:{CorrelationId}:{UserId}`
- Unique database constraint guarantees that concurrent or redelivered message consumer executions result in a no-op or detected duplicate rather than duplicate message dispatches.

### 2. Event-Driven Asynchronous Consumer
- Implemented `NotificationConsumerService` as a background worker listening on durable queue `resolveops.notifications` bound to fanout exchanges for integration events (`CaseSlaBreachedV1`, `ExceptionDetectedV1`, `ClaimDecisionRecordedV1`, `ClaimRecoveryRecordedV1`, `ClaimSubmittedV1`).
- Consumer applies the transactional inbox pattern (`InboxMessage`) before evaluating recipient routing rules.

### 3. Strict Compliance with Rule 10
- All database state changes (in-app notifications, delivery attempt rows initialized with `Pending` status) are committed to SQL Server FIRST.
- External email delivery via `IEmailSender` (`SmtpEmailSender`) and SignalR socket pushes execute AFTER the database transaction has committed.
- Upon completion of the SMTP dispatch, delivery status is updated to `Sent` (with provider message ID) or `Failed`/`Retrying` with error details.

### 4. SignalR Hub & Realtime Transport
- Hub route: `/hubs/notifications` mapped on the API host.
- Authentication: JWT authentication configured with `JwtBearerEvents.OnMessageReceived` extracting `access_token` from query parameters for WebSocket upgrade requests.
- Upon connection, clients automatically join `user_{userId}` and `tenant_{tenantId}` groups based on authenticated claims.
- `SignalRNotificationRealtimeService` publishes direct in-app notifications to `user_{userId}` and tenant-wide operational events to `tenant_{tenantId}`.

### 5. OpenTelemetry Metrics
- Registered `NotificationMetrics` (`ResolveOps.Notifications`):
  - `notifications.sent.total` (counter, tagged with channel)
  - `notifications.failures.total` (counter)
  - `notifications.email.duration.seconds` (histogram)
  - `notifications.realtime.active_connections` (up-down counter tracking SignalR connections)

---

## Consequences

- Zero risk of duplicate emails or duplicate in-app alerts on broker redeliveries or worker crashes.
- High database throughput: slow external SMTP socket connections never hold SQL database locks.
- Realtime operational visibility for Angular web clients and mobile operators.
- Clean separation between business domain entities, email transport adapters, and real-time presentation.

# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 13.

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
- Section 0: Agent rules and non-negotiable constraints (Rule 4: AI is strictly forbidden from financial state transitions; Rule 10: no database transactions around slow external HTTP, message publication, or email delivery; Rule 13: treat inbound webhooks/queue messages as untrusted; Rule 14: tenant isolation; Rule 15: cancellation tokens; Rule 16: structured logging without PII or credentials)
- Section 4.1 & 4.2: Recipient personas and roles (Logistics Coordinator, Exception Specialist, Claims Specialist, Operations Manager, Finance, Carrier External)
- Section 8.8 – 8.11: Operational events requiring notifications:
  - SLA breach alerts (`CaseSlaBreachedV1`)
  - Exception detection & high severity escalations (`ExceptionDetectedV1`)
  - Claim review requests & submission approvals
  - Carrier claim response recordings (`ClaimDecisionRecordedV1`)
  - Recovery transactions & settlements (`ClaimRecoveryRecordedV1`)
- Section 10.6: Tenant invariants (tenant isolation on all queries, commands, notification channels, SignalR groups, and cache keys; use `AppDbContext` global query filters without redundant `.Where(x => x.TenantId == tenantId)`)
- Section 11: Edge cases (Edge Case 13: Notification sends, worker crashes before local success flag -> Provider idempotency key and outbox delivery log prevent duplicate business notifications where possible; Edge Case 14: queue message redelivery handled idempotently)
- Section 12: Architecture and module structure (`ResolveOps.Modules.Notifications`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`, `ResolveOps.Security`, `ResolveOps.Worker`)
- Section 15: Notification tables (`notification_templates`, `notifications`, `notification_preferences`, `notification_deliveries`)
- Section 16: Notification endpoints (`GET /api/notifications`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`, `GET /api/notifications/preferences`, `PUT /api/notifications/preferences`)
- Section 17.3 & 17.4: Core integration events and RabbitMQ queue topology (`resolveops.notifications` queue)
- Section 18.1: Background worker (`NotificationWorker` / `NotificationConsumerService`, `IEmailSender`, template rendering, delivery retries)
- Section 24: Implementation roadmap and Phase 13 definition
- Section 25: Coding standards (C# 13 / .NET 10, explicit mapping, no generic repo, 1 class per file)
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
| Realtime WebSockets | ASP.NET Core SignalR (`NotificationHub`) |
| Email Service | `IEmailSender` abstraction (Mailpit SMTP for local development: port 1025; Brevo/SendGrid configurable for production) |
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

**Current phase:** `Phase 13 — Notifications and realtime operations`

**Phases already completed:** `Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity), Phase 3 (partners/locations/calendar), Phase 4 (shipment domain), Phase 5 (messaging/outbox/inbox/RabbitMQ), Phase 6 (tracking ingestion & normalization), Phase 7 (exception policy engine & case creation), Phase 8 (exception case workflow, tasks, SLA), Phase 9 (evidence and secure document pipeline), Phase 10 (claim eligibility and draft claims), Phase 11 (claim approval, submission, response, appeal), Phase 12 (financial recovery and settlement)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings).
- Architecture tests pass (5 passed, 0 failed).
- Code formatting passes strict verification (dotnet format ResolveOps.slnx --verify-no-changes exits with code 0).
- Clean Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Strict 1-class-per-file convention enforced across all vertical slices (<Feature>Command.cs, <Feature>Endpoint.cs, <Feature>Handler.cs, <Feature>Validator.cs, <Feature>Response.cs). Do NOT combine multiple classes into one file.
- Auto-Discovery implemented for Modules, Endpoints, and Handlers (AddHandlersFromAssembly, MapEndpointsFromAssembly). Endpoints implement IEndpoint.
- Standardized User & Tenant resolution: Endpoints MUST use httpContext.GetUserId() or httpContext.TryGetUserId() and httpContext.GetTenantId() from ResolveOps.Security extensions. Do NOT manually parse ClaimTypes.NameIdentifier or sub.
- Domain Constants: All domain statuses and types MUST be defined as public static class with public const string fields, NOT C# enums.
- Global Tenant Query Filters: AppDbContext centrally handles tenant isolation via HasQueryFilter. Handlers MUST NOT append manual .Where(x => x.TenantId == tenantId) filters.
- Observability Location: All metrics classes (e.g. NotificationMetrics.cs, ClaimMetrics.cs) MUST be placed in src/BuildingBlocks/ResolveOps.Observability, NOT inside module projects.
- Concurrency Stamp: Centralized optimistic concurrency via string ConcurrencyStamp in AppDbContext.ApplyAuditAndConcurrency() (domain entity methods MUST NOT manually mutate or roll stamps).
- Database schema: Migrations through 20260922153840_AddFinancialRecoveryAndSettlement are applied.
- Background workers running in ResolveOps.Worker:
  - TrackingIngestionConsumerService (RabbitMQ v7 async consumer for raw carrier receipts)
  - ExceptionEvaluationConsumerService (RabbitMQ v7 async consumer for TrackingEventAcceptedV1)
  - MissedDeadlineScanJob (Quartz.NET periodic scan for missed shipment milestones)
  - SlaBreachScanJob (Quartz.NET periodic scan for breached SLA clocks)
  - ClaimFollowUpScanJob (Quartz.NET periodic scan for SLA follow-up deadlines)
  - DocumentProcessingWorker (BackgroundService for document malware scanning)
  - AbandonedUploadCleanupJob (Quartz.NET periodic cleanup of expired upload intents)
- Tests: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so ALL UT/IT test requirements are currently waived. Architecture tests remain strictly required.
```

**Stopping point / specific task this session:**
```
Implement Phase 13: Notifications and realtime operations according to Master Spec §24 Phase 13, §0 (Rule 10), §4.1, §4.2, §8.8–8.11, §10.6, §11 (Edge Case 13, 14), §12, §15, §16, §17.3, §17.4, §18.1, §25:

1. Domain Entities & Value Objects (Spec §15):
   In src/BuildingBlocks/ResolveOps.Domain/Notifications/:
   - NotificationChannel (public static class with const string, NOT enums):
     - InApp = "InApp", Email = "Email". Provide IReadOnlyList<string> All, bool IsValid(string? channel).
   - NotificationClass (public static class with const string, NOT enums):
     - SlaBreachAlert = "SlaBreachAlert" (Mandatory)
     - ExceptionEscalation = "ExceptionEscalation" (Mandatory)
     - TaskAssigned = "TaskAssigned" (Subscribed)
     - ClaimReviewRequested = "ClaimReviewRequested" (Mandatory)
     - ClaimDecisionReceived = "ClaimDecisionReceived" (Subscribed)
     - ClaimRecoveryRecorded = "ClaimRecoveryRecorded" (Subscribed)
     - Provide IReadOnlyList<string> All, bool IsMandatory(string notificationClass), bool IsValid(string? notificationClass).
   - DeliveryStatus (public static class with const string, NOT enums):
     - Pending = "Pending", Sent = "Sent", Failed = "Failed", Retrying = "Retrying".
   - NotificationTemplate:
     - Fields: Id (Guid), TenantId (Guid? - null for system default), TemplateCode (string max 100), Channel (string max 30), Version (int), SubjectTemplate (string max 250), BodyTemplate (string nvarchar(max)), IsActive (bool), IAuditableEntity properties.
     - Method Render(IReadOnlyDictionary<string, string> placeholders) returning (string Subject, string Body).
   - Notification (In-App Notification):
     - Fields: Id (Guid), TenantId (Guid), UserId (Guid), NotificationClass (string max 50), Channel (string max 30), Title (string max 250), Message (string nvarchar(max)), DataJson (string? nvarchar(max)), IsRead (bool), ReadAtUtc (DateTimeOffset?), CreatedAtUtc (DateTimeOffset), IAuditableEntity properties.
     - Methods: MarkAsRead(TimeProvider timeProvider).
   - NotificationPreference:
     - Fields: Id (Guid), TenantId (Guid), UserId (Guid), NotificationClass (string max 50), Channel (string max 30), IsEnabled (bool), IsMandatory (bool).
     - Method SetEnabled(bool enabled): Cannot disable if IsMandatory is true (returns Failure or DomainError).
   - NotificationDelivery:
     - Fields: Id (Guid), TenantId (Guid), NotificationId (Guid?), Channel (string max 30), RecipientAddress (string max 200), ProviderName (string max 50), ProviderMessageId (string? max 150), IdempotencyKey (string max 150), Status (string max 30), AttemptCount (int), LastError (string?), SentAtUtc (DateTimeOffset?), CreatedAtUtc (DateTimeOffset).
     - Factory method & state methods: MarkSent(string providerMessageId, TimeProvider timeProvider), RecordFailure(string error, bool canRetry, TimeProvider timeProvider).

2. Email Provider Abstraction & Infrastructure (Spec §24 Phase 13):
   In src/BuildingBlocks/ResolveOps.Application/Notifications/ or ResolveOps.Modules.Notifications/:
   - IEmailSender:
     - Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken);
   - SmtpEmailSender (implements IEmailSender):
     - Configurable via SmtpOptions (Host, Port, Username, Password, EnableSsl, FromEmail, FromName).
     - Compatible with Mailpit local SMTP (host: localhost, port: 1025, no auth) and production SMTP providers (Brevo / SendGrid).
     - Rule 10: Email sends must NOT be executed inside an open database transaction.

3. Realtime SignalR Hub (Spec §24 Phase 13):
   In src/Modules/ResolveOps.Modules.Notifications/Hubs/ or ResolveOps.Api:
   - NotificationHub : Hub
     - Route: /hubs/notifications
     - Uses JWT authentication via query string access_token or authorization header.
     - OnConnectedAsync: Adds connection to tenant group ("tenant_{tenantId}") and user personal group ("user_{userId}").
     - OnDisconnectedAsync: Cleans up groups.
   - INotificationRealtimeService / HubContext wrapper:
     - Task SendNotificationToUserAsync(Guid tenantId, Guid userId, object notificationPayload, CancellationToken ct);
     - Task BroadcastToTenantAsync(Guid tenantId, string eventType, object payload, CancellationToken ct);

4. Background Notification Worker (Spec §18.1, §24 Phase 13):
   In src/Hosts/ResolveOps.Worker/:
   - NotificationConsumerService (BackgroundService / RabbitMQ Consumer):
     - Consumes from queue `resolveops.notifications` bound to exchange `resolveops.events`.
     - Subscribes to events: CaseSlaBreachedV1, ExceptionDetectedV1, ClaimDecisionRecordedV1, ClaimRecoveryRecordedV1, ClaimSubmittedV1.
     - For each event:
       - Determines target recipient users (e.g. case assignee, team members, claim preparer, finance role).
       - Checks user preferences (honors unsubscribe unless class is mandatory).
       - Generates stable idempotency key: $"{tenantId}:{event.EventType}:{event.EventId}:{userId}:{channel}".
       - Edge Case 13 & 14: Checks notification_deliveries with unique idempotency key before attempting send.
       - Dispatches in-app notification record and pushes via SignalR Hub.
       - If Email channel subscribed: sends email via IEmailSender, records provider message ID and delivery attempt.
       - On transient failure: updates status to Retrying / Failed with attempt count and last error.

5. Persistence & EF Core Configuration (Spec §15):
   In src/BuildingBlocks/ResolveOps.Persistence/Configurations/:
   - NotificationTemplateConfiguration:
     - Table notification_templates. Unique index on (tenant_id, template_code, channel, version).
   - NotificationConfiguration:
     - Table notifications. Index on (tenant_id, user_id, is_read, created_at_utc).
   - NotificationPreferenceConfiguration:
     - Table notification_preferences. Unique index on (tenant_id, user_id, notification_class, channel).
   - NotificationDeliveryConfiguration:
     - Table notification_deliveries. Unique index on (tenant_id, idempotency_key).
   Update AppDbContext:
   - Add DbSets: NotificationTemplates, Notifications, NotificationPreferences, NotificationDeliveries.
   - Add global tenant query filters for each entity.
   - Generate EF Core Migration: AddNotificationsAndRealtimeOperations.

6. Notification REST APIs (ResolveOps.Modules.Notifications) (Spec §16):
   Strictly adhere to Vertical Slice Architecture with 1 class per file (Endpoint.cs, Handler.cs, Command.cs/Query.cs, Validator.cs, Response.cs):
   - GetNotifications: GET /api/notifications (Query params: isRead, page, pageSize. Returns paged notifications and unreadCount).
   - MarkNotificationRead: POST /api/notifications/{id:guid}/read (Marks specific notification as read, validates ownership).
   - MarkAllNotificationsRead: POST /api/notifications/read-all (Marks all unread notifications for current user as read).
   - GetNotificationPreferences: GET /api/notifications/preferences (Returns list of preferences and mandatory indicators).
   - UpdateNotificationPreferences: PUT /api/notifications/preferences (Updates enabled channels; rejects disabling mandatory classes).

7. Observability & Metrics (Spec §21):
   In src/BuildingBlocks/ResolveOps.Observability/NotificationMetrics.cs:
   - notifications.sent.total (Counter<long>, tags: channel, notification_class, status)
   - notifications.email.duration.seconds (Histogram<double>)
   - notifications.failures.total (Counter<long>, tags: channel, error_type)
   - notifications.realtime.active_connections (UpDownCounter<long>)

8. Architecture Decisions & Documentation:
   - Create docs/adr/ADR-030-notifications-and-realtime-operations.md.
   - Update AGENTS.md (Current Phase: Phase 13 Complete, Next: Phase 14) and CHANGELOG.md.

Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for Phase 13 from the specification.
3. **Implement only the Phase 13 scope** — do not add features from future phases (e.g., reporting dashboards / scorecards in Phase 14, frontend in Phase 15).
4. **Preserve** Modular Monolith and Vertical Slice boundaries (`ResolveOps.Modules.Notifications`, `ResolveOps.Domain`, `ResolveOps.Application`, `ResolveOps.Persistence`, `ResolveOps.Observability`, `ResolveOps.Security`, `ResolveOps.Worker`).
5. **Enforce 1-class-per-file**: Every vertical slice feature MUST have separate files: `<Action><Resource>Endpoint.cs`, `<Action><Resource>Handler.cs`, `<Action><Resource>Command.cs` / `Query.cs`, `<Action><Resource>Validator.cs`, `<Action><Resource>Response.cs`.
6. **Use centralized security extensions**: Use `httpContext.GetUserId()` / `httpContext.GetTenantId()` in all endpoints. Do NOT manually extract claim strings.
7. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
8. **Ensure ConcurrencyStamp integrity**: Concurrency stamps are managed centrally by `AppDbContext.ApplyAuditAndConcurrency()`; domain methods MUST NOT manually mutate stamps.
9. **Add/update** EF Core migration `AddNotificationsAndRealtimeOperations`, OpenTelemetry instrumentation in `ResolveOps.Observability`, and ADR-030.
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
- Do NOT use database transactions around email sending, message publication, or external HTTP calls (Rule 10).
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Do NOT use `float` or `double` for monetary values — always use `decimal` or the `Money` value object.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests remain mandatory.

## Definition of Done Checklist (Section 31 & §24 Phase 13)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] All features follow strict 1-class-per-file convention in `ResolveOps.Modules.Notifications`
- [ ] Domain constants defined as `public static class` with `public const string` constants (NO enums)
- [ ] `NotificationTemplate`, `Notification`, `NotificationPreference`, and `NotificationDelivery` entities implemented with guarded methods
- [ ] Unique constraint on `(TenantId, IdempotencyKey)` strictly prevents duplicate notification and email dispatches (Edge Case 13)
- [ ] User notification preferences enforce non-disableable mandatory notification classes (SLA breaches, critical escalations)
- [ ] `IEmailSender` and `SmtpEmailSender` implemented and configurable for local Mailpit SMTP (port 1025) and production
- [ ] SignalR `NotificationHub` registered at `/hubs/notifications` with JWT authentication and tenant/user group management
- [ ] `NotificationConsumerService` in `ResolveOps.Worker` processes integration events from RabbitMQ and dispatches in-app + email notifications
- [ ] Email delivery executes outside database transactions (Rule 10)
- [ ] REST API endpoints in `ResolveOps.Modules.Notifications` implemented with Minimal APIs & FluentValidation
- [ ] All endpoints use `httpContext.GetUserId()` / `httpContext.GetTenantId()` from `ResolveOps.Security`
- [ ] Module auto-discovery preserved (`AddHandlersFromAssembly`, `MapEndpointsFromAssembly`)
- [ ] Handlers rely on `AppDbContext` global tenant filter (no redundant manual `.Where(x => x.TenantId == tenantId)`)
- [ ] EF Core configurations and migration `AddNotificationsAndRealtimeOperations` applied
- [ ] Metrics instrumented in `src/BuildingBlocks/ResolveOps.Observability/NotificationMetrics.cs`
- [ ] ADR-030 written in `docs/adr/`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Giá trị cho Phase 13 |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 13 — Notifications and realtime operations` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0 through Phase 12 (Financial recovery and settlement)` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Clean build Release 0 errors/warnings, ArchTests 5/5 pass, EF migration `20260922153840_AddFinancialRecoveryAndSettlement` |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai Notification templates, in-app notifications, preferences, email delivery worker, Mailpit SMTP, SignalR hub, delivery idempotency |

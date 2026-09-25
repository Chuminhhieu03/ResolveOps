# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt dưới đây và gửi cho agent để thực hiện Phase 16.

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
- Section 4.1 & 4.2: Recipient personas, permissions, and roles (Operations Manager, Claims Specialist, Logistics Coordinator, Finance, Carrier External)
- Section 10.6: Tenant invariants (tenant isolation on all queries, commands, caches, outbox/inbox messages, blob storage paths, and read models)
- Section 11 & Section 19.9: Edge cases & Security threat model vectors:
  - Broken object-level authorization (BOLA / IDOR)
  - Tenant leakage through query, cache, log, export, or blob
  - Token theft and refresh token family rotation / replay detection
  - Webhook spoofing, HMAC signature verification, timestamp & replay protection
  - Malicious file upload, magic byte verification, and malware quarantine pipeline
  - CSV formula injection protection (escaped with `'`)
  - Stored XSS in comments and document names
  - SQL injection defense in Dapper and dynamic reporting queries
  - Rate limiting, DoS defense, and queue flooding protection
  - Structured logging without PII, tokens, or document bodies
- Section 19: Security specification (§19.1–§19.9: Objectives, Authentication, Authorization RBAC policies, Tenant isolation in depth, Webhook security, File security, API security, AI boundary, Threat model checklist)
- Section 20: Observability, reliability, and performance:
  - §20.1 Correlation (TraceId, CorrelationId, CausationId, TenantId)
  - §20.2 Required structured audit logs
  - §20.3 Required OpenTelemetry metrics across API, Tracking, Exceptions, Claims, Messaging, Documents, Reporting
  - §20.6 Reliability patterns (Transactional outbox, Idempotent inbox, Bounded retry, DLX/DLQ, Circuit breaker, Graceful shutdown, Health checks)
  - §20.7 Reference workload: 100 tenants, 1,000,000 shipments, 3,000,000 legs, 20,000,000 tracking events, 100,000 exception cases, 25,000 claims; Sustained 100 eps, Burst 300 eps (5% duplicates, 10% out-of-order, 1% unmatched)
  - §20.8 API performance targets (p95 < 500ms open exceptions, p95 < 400ms exception detail, p95 < 300ms tracking receipt acceptance, p95 < 500ms case transition, p95 < 300ms claim readiness, p95 < 1,000ms dashboard summary)
  - §20.9 Performance techniques (batch inserts, compiled queries, read models, composite indexes, cursor pagination, asynchronous exports)
- Section 22: Testing strategy (§22.4 Performance testing, §22.7 Security testing)
- Section 24: Phase 16 definition, tasks, and Definition of Done
- Section 30: Operational runbooks (§30.1 Outbox backlog, §30.2 Dead-letter messages, §30.3 Quarantined tracking, §30.4 Stuck claim deadline job, §30.5 Blob/document incident, §30.6 Tenant data leak suspicion, §30.7 Database restore)
- `AGENTS.md` in the repository root and ADRs (ADR-001 through ADR-032)

## Technology Stack (mandatory — do not substitute)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 LTS, C# |
| Web API | ASP.NET Core Minimal APIs |
| ORM | EF Core 10 with SQL Server provider (`EnableRetryOnFailure`) |
| Query Engine | Dapper 2.1.66 (pinned) + EF Core Projections |
| Database | SQL Server 2022 (Docker: `mcr.microsoft.com/mssql/server:2022-latest`) |
| Message Broker | RabbitMQ 3.x (Docker: `rabbitmq:3-management`) — client: `RabbitMQ.Client v7` |
| Scheduler | Quartz.NET (in `ResolveOps.Worker`) |
| Object Storage | MinIO (Docker: `minio/minio`) — client: `AWSSDK.S3` (presigned URLs) |
| Cache | Redis 7 (Docker: `redis:7-alpine`) — client: `StackExchange.Redis` |
| Observability | OpenTelemetry + Serilog + Seq (`datalust/seq`) |
| Performance & Benchmarking | BenchmarkDotNet / NBomber or k6 / Testcontainers / `ResolveOps.PerformanceTests` |
| Security & Diagnostics | `dotnet list package --vulnerable`, threat modeling (STRIDE), log PII sanitizers |
| Concurrency | Optimistic Concurrency via `string ConcurrencyStamp` (ADR-006 & centralized `AppDbContext`) |

**Rejected (do not add):** Azure proprietary services, MassTransit, AutoMapper, generic repositories, microservices, Kubernetes, Kafka.

## Current Implementation State

**Current phase:** `Phase 16 — Performance, resilience, and security hardening`

**Phases already completed:** `Phase 0 through Phase 15 (Frontend production workflow)`

**Repository state summary:**
```
- Solution builds cleanly in Release mode with warnings as errors (0 errors, 0 warnings across all 29 .NET projects).
- Architecture tests pass (5 passed, 0 failed).
- Code formatting passes strict verification (dotnet format ResolveOps.slnx --verify-no-changes exits with code 0).
- Modular Monolith architecture with .NET 10 Minimal APIs and Vertical Slice Architecture.
- Database migrations fully applied through 20260923151333_AddReportingAndCarrierScorecards.
- Complete backend modules: Tenancy, Identity, Partners, Shipments, Integrations, Tracking, Exceptions, Workflow, Documents, Claims, Notifications, Reporting, Audit.
- Reporting subsystem refactored with Open/Closed Principle (OCP) strategy pattern:
  - Event Projectors: IReportingEventProjector strategy with dedicated projectors for ShipmentCreated, TrackingEventAccepted, ExceptionDetected (supporting multi-leg leg carrier resolution), ClaimSubmitted, ClaimDecisionRecorded, ClaimRecoveryRecorded.
  - Export Engine: IExportDataGenerator strategy with dedicated generators for ExceptionCases, Claims, and CarrierScorecards with CSV formula-injection escaping.
  - Endpoints: GET /api/dashboard/operations, GET /api/dashboard/claims, GET /api/reports/exception-ageing, GET /api/reports/sla-performance, GET /api/reports/carrier-scorecards, GET /api/reports/financial-recovery, POST /api/exports/exception-cases, POST /api/exports/claims, POST /api/exports/carrier-scorecards, GET /api/exports (Export history), GET /api/exports/{exportId}.
  - Export completion notifications: in-app Notification persistence and real-time SignalR push (NotificationClass.ExportCompleted).
- Background workers running in ResolveOps.Worker:
  - TrackingIngestionConsumerService (RabbitMQ v7 async consumer)
  - ExceptionEvaluationConsumerService (RabbitMQ v7 async consumer)
  - NotificationConsumerService (RabbitMQ v7 async consumer with transient retry & DLX)
  - ReportingProjectionConsumerService (RabbitMQ v7 async consumer with OCP projectors)
  - MissedDeadlineScanJob (Quartz.NET periodic scan)
  - SlaBreachScanJob (Quartz.NET periodic scan)
  - ClaimFollowUpScanJob (Quartz.NET periodic scan)
  - NotificationEmailRetryScanJob (Quartz.NET periodic scan)
  - DocumentProcessingWorker (BackgroundService for virus scanning)
  - AbandonedUploadCleanupJob (Quartz.NET periodic cleanup)
  - ExportProcessingJob (Quartz.NET periodic CSV export generation outside DB transactions)
- Performance test project ready in tests/ResolveOps.PerformanceTests.
- Tests note: The user explicitly stated "Tôi không cần UT hay IT Test đâu" (I do not need UT or IT tests), so traditional unit/integration tests for every feature are waived. However, performance benchmarks, load test harnesses, security threat models, and architecture tests remain strictly required.
```

**Stopping point / specific task this session:**
```
Implement Phase 16: Performance, resilience, and security hardening according to Master Spec §24 Phase 16, §0, §4.1, §4.2, §10.6, §11, §19, §20, §22, §25, §30:

1. Synthetic Reference Dataset Generator (§20.7, §24 Phase 16):
   In tests/ResolveOps.PerformanceTests/ or a dedicated CLI seeding utility:
   - Implement a high-performance synthetic data generator capable of generating the reference dataset:
     - Configurable volume: Tenants (up to 100), Shipments (up to 1M), Shipment Legs (up to 3M), Tracking Events (up to 20M), Exception Cases (up to 100K), Claims (up to 25K), Timeline Entries, and Carrier Performance Snapshots.
     - Uses SqlBulkCopy or optimized batch inserts for rapid local seeding without transaction timeout.
     - Supports deterministic seeds for repeatable benchmarking.

2. Ingestion & Analytical Query Performance Testing (§20.7, §20.8, §24 Phase 16):
   - Ingestion throughput workload benchmark:
     - Test sustained 100 events/sec and burst 300 events/sec for tracking event ingestion.
     - Inject 5% duplicate tracking numbers/payloads (verifying inbox deduplication).
     - Inject 10% out-of-order timestamps and 1% unmatched payloads (verifying integration quarantine).
   - API p95 performance verification against Master Spec §20.8 targets:
     - GET /api/exceptions/cases (open exceptions first page): target p95 < 500 ms
     - GET /api/exceptions/cases/{id} (exception detail without document bytes): target p95 < 400 ms
     - GET /api/exceptions/cases/{id}/timeline (timeline first page): target p95 < 400 ms
     - POST /api/tracking/events (tracking receipt durable acceptance): target p95 < 300 ms
     - POST /api/exceptions/cases/{id}/triage (case transition): target p95 < 500 ms
     - Claim readiness calculation: target p95 < 300 ms
     - GET /api/dashboard/operations (dashboard summary via read models): target p95 < 1,000 ms
   - Profile CPU, memory allocations, slow queries, and missing database indexes.
   - Apply database index optimizations, compiled queries, or Dapper tuning where measured bottlenecks occur.
   - Commit reproducible measurements, before/after results, and hardware specifications in docs/performance/PERFORMANCE_REPORT.md.

3. Resilience & Chaos Engineering Verification (§20.6, §24 Phase 16):
   - Broker Outage & Outbox Backlog Recovery:
     - Simulate RabbitMQ broker outage while operations create shipments, ingest tracking, and transition cases.
     - Verify business transactions succeed and events accumulate safely in the outbox_messages table.
     - Restore RabbitMQ connection: verify outbox publisher drains backlog automatically, with zero lost events and zero duplicate consumer side effects (via Inbox deduplication).
   - Database Transient Fault Resilience:
     - Verify EF Core SQL Server connection resilience (EnableRetryOnFailure configured with exponential backoff).
   - Poison Message & Dead-Letter Queue (DLQ) Handling:
     - Verify unprocessable/poison messages are routed to resolveops.dlx / resolveops.dead-letter with failure reason and delivery count headers.
     - Verify consumer does not crash or loop infinitely on malformed payloads.

4. Security Hardening & Threat Modeling (§19.1–§19.9, §24 Phase 16):
   - Deliver comprehensive Threat Model in docs/security/THREAT_MODEL.md analyzing all 15 vectors from Spec §19.9:
     - Broken Object-Level Authorization (BOLA / IDOR)
     - Cross-tenant data leakage (queries, cache, logs, exports, MinIO blobs)
     - Token theft and refresh token replay detection
     - Webhook spoofing, HMAC signature verification, replay protection
     - Malicious file uploads, mime/magic-bytes verification, virus scan sandbox
     - CSV formula injection protection (escaped with ')
     - Stored XSS in case comments / document metadata
     - SQL injection defense across Dapper and EF Core queries
     - Rate limiting & DoS defense across public webhook and ingestion endpoints
     - Queue flooding & message bomb mitigation
     - Secrets in source control or telemetry redaction
     - Insecure direct blob URLs (enforced private buckets + short-lived presigned URLs)
   - Tenant Isolation Verification:
     - Automated test harness verifying that Tenant A cannot access, query, export, download, or mutate Tenant B's shipments, cases, claims, documents, or reports (verifying 403 Forbidden / 404 Not Found).
   - Log Sanitization Audit:
     - Audit Serilog and OpenTelemetry log outputs to guarantee that access tokens, passwords, credit card/bank details, PII, and raw document contents are never logged.
   - Dependency Vulnerability Scan:
     - Run dotnet list package --vulnerable and ensure zero critical/high vulnerabilities exist.

5. Seven Operational Runbooks (Spec §30, §24 Phase 16):
   Create tested, actionable, step-by-step markdown runbooks in docs/runbooks/:
   - 30.1-outbox-backlog.md: Identifying oldest pending message, inspecting broker health, safe replay, escalation thresholds.
   - 30.2-dead-letter-replay.md: Inspecting DLQ reasons, classifying transient vs. poison, correcting data, replaying to destination queue.
   - 30.3-quarantined-tracking-events.md: Inspecting quarantine receipts, manual leg matching, replaying via API, preventing repeated mismatch.
   - 30.4-stuck-claim-deadline-job.md: Diagnosing Quartz scheduler, manual scan invocation, verifying idempotent notifications.
   - 30.5-document-incident-revocation.md: Revoking download intents, quarantining compromised MinIO objects, access log forensics.
   - 30.6-tenant-data-leak-incident.md: Immediate containment, credential rotation, forensic audit log analysis, regression prevention.
   - 30.7-database-restore-and-rpo-rto.md: Point-in-time restore procedure, migration validation, tenant financial integrity checks, measured RPO/RTO.

6. Architecture Decisions & Documentation:
   - Create docs/adr/ADR-033-performance-resilience-and-security-hardening.md.
   - Update AGENTS.md (Current Phase: Phase 16 Complete, Next: Phase 17) and CHANGELOG.md.

Do NOT write traditional Unit Tests or Integration Tests for every feature (waived by user). Architecture tests, performance benchmark harnesses, and build verification with 0 warnings/errors remain mandatory.
```

## Your Task

1. **Inspect** the existing repository, performance test stubs in `tests/ResolveOps.PerformanceTests`, and documentation folders in `docs/performance`, `docs/security`, and `docs/runbooks`.
2. **Implement reference dataset generator** in `tests/ResolveOps.PerformanceTests` supporting high-speed batch seeding.
3. **Execute performance benchmarks** for sustained/burst ingestion and API p95 response times. Profile bottlenecks, apply query/index optimizations, and record results in `docs/performance/PERFORMANCE_REPORT.md`.
4. **Verify resilience scenarios**: test broker outage, outbox backlog draining, idempotent inbox deduplication, and DLQ routing.
5. **Execute security audit & threat modeling**: deliver `docs/security/THREAT_MODEL.md` covering all 15 spec vectors, implement automated cross-tenant isolation tests, verify log PII redaction, and run dependency vulnerability scan.
6. **Author all 7 operational runbooks** in `docs/runbooks/` matching Spec §30.
7. **Ensure clean compilation**: Run `dotnet build ResolveOps.slnx -c Release` (0 warnings, 0 errors).
8. **Verify architecture integrity**: Run `dotnet test tests/ResolveOps.ArchitectureTests/ -c Release` (5/5 passed).
9. **Verify formatting**: Run `dotnet format ResolveOps.slnx --verify-no-changes` (exits with code 0).
10. **Write** `docs/adr/ADR-033-performance-resilience-and-security-hardening.md`.
11. **Update** `CHANGELOG.md` and `AGENTS.md` with Phase 16 completion status.
12. **Report** at the end: files created/modified, benchmark results, threat model summary, runbook status, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT allow an LLM or AI to approve, reject, pay, or execute financial transitions on claims (Rule 4 & Invariant 12).
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks.
- Do NOT use database transactions around file uploads, message publication, or external HTTP calls (Rule 10).
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Do NOT use `float` or `double` for monetary values — always use `decimal` or the `Money` value object.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- Do NOT write Unit Tests or Integration Tests (waived by user). Architecture tests, performance benchmarks, and security verification remain mandatory.

## Definition of Done Checklist (Section 31 & §24 Phase 16)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`dotnet build ResolveOps.slnx --configuration Release`)
- [ ] Architecture tests pass with 0 failures (`dotnet test tests/ResolveOps.ArchitectureTests/ --configuration Release`)
- [ ] Formatting verification passes (`dotnet format ResolveOps.slnx --verify-no-changes`)
- [ ] Reference dataset generator implemented with batch insert optimization
- [ ] Ingestion throughput tested (sustained 100 eps, burst 300 eps, with duplicates, out-of-order, and unmatched)
- [ ] API p95 response time targets verified against Spec §20.8:
  - [ ] Open exceptions list: p95 < 500 ms
  - [ ] Exception detail: p95 < 400 ms
  - [ ] Timeline entries: p95 < 400 ms
  - [ ] Tracking receipt durable acceptance: p95 < 300 ms
  - [ ] Case triage transition: p95 < 500 ms
  - [ ] Claim readiness calculation: p95 < 300 ms
  - [ ] Operations dashboard summary: p95 < 1,000 ms
- [ ] Performance report committed with reproducible before/after results in `docs/performance/PERFORMANCE_REPORT.md`
- [ ] Broker outage and outbox recovery verified: outbox backlog drains cleanly without message loss or duplicate side effects
- [ ] Database transient failure resilience verified (`EnableRetryOnFailure`)
- [ ] DLQ and dead-letter routing verified with delivery count and failure headers
- [ ] Threat model completed in `docs/security/THREAT_MODEL.md` addressing all 15 threat vectors in §19.9
- [ ] Tenant isolation test suite verifies cross-tenant boundary enforcement (queries, mutations, blobs, exports)
- [ ] File security verified (magic bytes validation, oversize protection, malware quarantine flow)
- [ ] Log sanitization verified (zero PII, credentials, tokens, or document bodies in logs/telemetry)
- [ ] Dependency scan verified (`dotnet list package --vulnerable` shows zero critical/high issues)
- [ ] All 7 operational runbooks written and verified in `docs/runbooks/`:
  - [ ] `30.1-outbox-backlog.md`
  - [ ] `30.2-dead-letter-replay.md`
  - [ ] `30.3-quarantined-tracking-events.md`
  - [ ] `30.4-stuck-claim-deadline-scan.md`
  - [ ] `30.5-document-incident-revocation.md`
  - [ ] `30.6-tenant-data-leak-incident.md`
  - [ ] `30.7-database-restore-and-rpo-rto.md`
- [ ] ADR-033 written in `docs/adr/ADR-033-performance-resilience-and-security-hardening.md`
- [ ] `AGENTS.md` and `CHANGELOG.md` updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Giá trị cho Phase 16 |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 16 — Performance, resilience, and security hardening` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0 through Phase 15 (Frontend production workflow)` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Clean build Release 0 errors/warnings (29 projects), ArchTests 5/5 pass, format verified, full backend modules & background workers active, OCP projectors & export generators implemented |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | Triển khai bộ sinh dữ liệu mẫu tham chiếu (Reference dataset generator), kiểm thử hiệu năng ingestion (100–300 eps) và p95 latency API, kiểm thử độ chịu lỗi broker outage/outbox draining/DLQ, hoàn thiện Threat Model 15 vectors trong `docs/security/THREAT_MODEL.md`, kiểm tra cô lập đa tenant (Cross-tenant isolation), kiểm tra log không lộ PII/token, và viết 7 runbooks vận hành trong `docs/runbooks/` |


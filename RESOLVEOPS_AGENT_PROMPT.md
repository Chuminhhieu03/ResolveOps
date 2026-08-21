# ResolveOps — Agent Execution Prompt

> **Hướng dẫn dùng:** Copy toàn bộ prompt này, điền vào các trường `[IN DẬM NHƯ THẾ NÀY]`, rồi gửi cho agent.

---

## PROMPT (copy từ đây)

---

You are implementing **ResolveOps**, a Logistics Exception & Carrier Claims management platform.

## Specification

The root specification is:
```
c:\FSoft\ResolveOps\LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md
```

Read the specification before making any changes. Pay special attention to:
- Section 0: Agent rules and non-negotiable constraints
- Section 12: Architecture and module structure
- Section 13: Technology stack
- Section 15: Domain model and database design
- Section 17: Outbox/Inbox messaging patterns
- Section 24: Implementation roadmap and the current phase definition
- Section 25: Coding standards
- `AGENTS.md` in the repository root (if it exists)

## Technology Stack (mandatory — do not substitute)

| Layer | Technology |
|---|---|
| Runtime | .NET 10 LTS, C# |
| Web API | ASP.NET Core Minimal APIs |
| ORM | EF Core 10 with SQL Server provider |
| Database | SQL Server 2022 (Docker: `mcr.microsoft.com/mssql/server:2022-latest`) |
| Message Broker | RabbitMQ 3.x (Docker: `rabbitmq:3-management`) — client: `RabbitMQ.Client v7` |
| Object Storage | MinIO (Docker: `minio/minio`) — client: `AWSSDK.S3` |
| Cache | Redis 7 (Docker: `redis:7-alpine`) — client: `StackExchange.Redis` |
| Observability | OpenTelemetry + Serilog + Seq (`datalust/seq`) |
| Local Orchestration | .NET Aspire + Docker Compose |
| Frontend | Angular 19+ + TypeScript + Angular Material + Angular Reactive Forms |
| Testing | xUnit + Testcontainers (.NET) + WireMock.Net + Playwright |
| CI/CD | GitHub Actions + GHCR + Docker Compose on VPS |

**Rejected (do not add):** Azure services, MassTransit, AutoMapper, generic repositories, React/Vite, microservices, Kubernetes, Kafka.

## Current Implementation State

**Current phase:** `[VÍ DỤ: Phase 3 — Partners, locations, and business calendar]`

**Phases already completed:** `[VÍ DỤ: Phase 0 (structure), Phase 1 (foundation), Phase 2 (tenancy/identity)]`

**Repository state summary:**
```
[MÔ TẢ NGẮN TRẠNG THÁI HIỆN TẠI CỦA REPO — ví dụ:
- Solution builds successfully
- EF Core migrations: 001_InitialSchema, 002_TenantSettings, 003_Identity
- Tests passing: 47 unit, 12 integration
- AGENTS.md updated to Phase 2 complete
- Known issue: refresh token rotation test is skipped (reason: X)
]
```

**Stopping point / specific task this session:**
```
[MÔ TẢ CỤ THỂ — ví dụ:
- Stopped after implementing CarrierPartner aggregate
- Need to implement: BusinessCalendar with working days and holiday overrides
- Then implement: SLA calculation using business calendar
- Then: integration tests for cross-midnight and holiday scenarios
]
```

## Your Task

1. **Inspect** the existing repository and summarize its current state (files changed, migrations, tests passing).
2. **Identify** the exact deliverables and Definition of Done for the current phase from the specification.
3. **Implement only the current phase scope** — do not add features from future phases.
4. **Preserve** Modular Monolith and Vertical Slice boundaries (Section 12).
5. **Apply all coding standards** from Section 25 (no `.Result`, no empty catch, use `CancellationToken`, `TimeProvider`, `DateTimeOffset`, explicit mapping).
6. **Add/update** EF Core migrations, unit tests, integration tests, OpenTelemetry instrumentation, and ADRs required by the spec.
7. **Run** formatting (`dotnet format`), build (`dotnet build`), unit tests, architecture tests, and integration tests.
8. **Fix** any failures caused by your changes before reporting done.
9. **Update** `CHANGELOG.md` and `AGENTS.md` with current phase status.
10. **Report** at the end: files changed, commands run, test results, assumptions made, and remaining risks.

## Non-negotiable Rules (from Section 0 of spec)

- Do NOT add microservices, AI features, a generic repository, or unrelated features.
- Do NOT bypass business invariants, tenant isolation, concurrency, idempotency, or security checks to make tests pass.
- Do NOT use AutoMapper — mapping must be explicit.
- Do NOT put business logic in endpoints.
- Do NOT use `.Result`, `.Wait()`, or sync-over-async.
- Do NOT commit secrets, connection strings, or PII.
- Do NOT use `DateTime.UtcNow` directly in testable business logic — use `TimeProvider`.
- Empty catch blocks are forbidden.
- If a requirement is ambiguous: choose the simplest reversible behavior, record the assumption in code comments and AGENTS.md, and continue.
- If a requested task is in the deferred list (Section 28.3), create a backlog entry and do not implement it.

## Definition of Done Checklist (Section 31)

Before marking the phase complete, verify:
- [ ] Build succeeds with warnings as errors (`-warnaserror`)
- [ ] No placeholder/TODO/empty catch in delivered code
- [ ] Migrations exist for all schema changes
- [ ] Tenant-aware query filters applied
- [ ] Concurrency token on all mutable aggregates
- [ ] Authorization policy defined on all endpoints
- [ ] OpenAPI contract updated
- [ ] Outbox used where business event is linked to a transaction
- [ ] Idempotent consumer with inbox record
- [ ] Tenant isolation tests pass
- [ ] Unit and integration tests cover success and failure paths
- [ ] AGENTS.md updated with phase status

---

## HƯỚNG DẪN ĐIỀN PROMPT

### Trường bắt buộc điền mỗi lần:

| Trường | Mô tả | Ví dụ |
|---|---|---|
| `[Current phase]` | Phase đang làm theo Section 24 | `Phase 5 — Outbox, inbox, and broker foundation` |
| `[Phases already completed]` | Danh sách phase đã xong | `Phase 0, 1, 2, 3, 4` |
| `[Repository state summary]` | Tình trạng repo hiện tại | Số migration, số test, file nào đang có |
| `[Stopping point]` | Bạn đang dừng ở đâu và muốn làm gì tiếp | "Xong Shipment aggregate, cần làm tracking webhook" |

### Cách lấy repository state nhanh:

Chạy lệnh này trong repo để lấy thông tin điền vào:
```powershell
# Chạy trong thư mục repo
Write-Host "=== BUILD ===" ; dotnet build --no-restore -q 2>&1 | tail -3
Write-Host "=== TESTS ===" ; dotnet test --no-build -q 2>&1 | tail -5  
Write-Host "=== MIGRATIONS ===" ; dotnet ef migrations list --project src/Infrastructure 2>&1
Write-Host "=== GIT STATUS ===" ; git log --oneline -5
```

### Ví dụ prompt đầy đủ đã điền:

```
Current phase: Phase 5 — Outbox, inbox, and broker foundation

Phases already completed: Phase 0 (project structure + ADRs), Phase 1 (Aspire + EF Core + SQL Server + OpenTelemetry), Phase 2 (tenancy + ASP.NET Identity + JWT), Phase 3 (carriers, customers, business calendar), Phase 4 (shipments + planned milestones)

Repository state summary:
- dotnet build: SUCCESS (0 warnings)
- Tests: 89 passing (62 unit, 27 integration), 0 failing
- Migrations: 001_InitialSchema → 007_ShipmentMilestones
- AGENTS.md: Phase 4 marked complete
- Angular project: not started yet (starts Phase 15)

Stopping point:
- Completed Phase 4 — all tests passing
- Starting Phase 5: need to implement SQL Server Outbox + RabbitMQ publisher + Inbox consumer
- First task: implement OutboxMessage table, OutboxPublisherService (BackgroundService), and verify at-least-once delivery with integration test
```

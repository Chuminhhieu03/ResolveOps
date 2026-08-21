# Test Strategy

> Source: ResolveOps Master Specification, Section 22.
> This document describes the testing pyramid, coverage policy, and test requirements for each layer.

---

## Testing Pyramid

```
                    ┌─────────────────────┐
                    │  End-to-End Tests   │  (Playwright — critical journeys only)
                    └─────────────────────┘
                  ┌──────────────────────────┐
                  │   Performance Tests       │  (k6 or NBomber — Phase 16)
                  └──────────────────────────┘
                ┌──────────────────────────────┐
                │  Security / Isolation Tests   │  (authorization, IDOR, tenant leakage)
                └──────────────────────────────┘
              ┌────────────────────────────────────┐
              │        Contract Tests               │  (WireMock.Net — carrier adapters, events)
              └────────────────────────────────────┘
            ┌──────────────────────────────────────────┐
            │          Integration Tests                │  (Testcontainers — real SQL Server/RabbitMQ/Redis)
            └──────────────────────────────────────────┘
          ┌────────────────────────────────────────────────┐
          │              Unit Tests                          │  (fast, exhaustive domain tests)
          └────────────────────────────────────────────────┘
```

---

## Test Layers

### 1. Domain Unit Tests (`tests/ResolveOps.UnitTests/`)

**Scope:** Aggregate state machines, value objects, business invariants, domain events.

**Requirements per aggregate:**
- Successful state transition.
- Every prohibited previous state.
- Missing permission or precondition (domain-relevant).
- Boundary monetary values.
- Expected domain events are raised.
- Expected audit-relevant data is recorded.
- Expected version behavior (optimistic concurrency token).

**Key test names follow:** `<Subject>_<Scenario>_<ExpectedOutcome>`

```
ExceptionCase_Assign_WhenDetected_ShouldMoveToAssigned
ExceptionCase_Assign_WhenAlreadyClosed_ShouldFail
Claim_RequestReview_WhenMandatoryEvidenceMissing_ShouldFail
Claim_RecordRecovery_WithDuplicateExternalReference_ShouldFail
Money_AddDifferentCurrencies_ShouldFail
```

### 2. Application Tests

**Scope:** Feature handlers with controlled, substituted dependencies (in-memory or fakes).

**Focus:** Validation, authorization checks, mapping, handler logic, outbox insertion.

### 3. Integration Tests (`tests/ResolveOps.IntegrationTests/`)

**Scope:** Real SQL Server (Testcontainers), real Redis, real RabbitMQ.

**Required scenarios:**
```
TwoConcurrentCaseAssignments_OneSucceedsOneReturnsConflict
EventAndScheduledScan_CreateSameFingerprint_OnlyOneCaseExists
DuplicateWebhook_OnlyOneReceiptBusinessEffect
OutboxAndAggregateCommitTogether
ConsumerFailure_RollsBackInboxAndBusinessChanges
CrossTenantIdQuery_ReturnsNotFound
PolicyVersionActivated_OldOpenCaseRetainsOriginalVersion
DuplicateRecoveryReference_IsRejectedByDatabaseConstraint
```

### 4. Architecture Tests (`tests/ResolveOps.ArchitectureTests/`)

**Scope:** Module dependency rules enforced via reflection (NetArchTest.Rules).

**Required checks:**
- Domain projects do not reference Infrastructure.
- Application projects do not reference Infrastructure directly.
- Modules do not directly reference other modules' internal types.
- No `DateTime.UtcNow` in Domain or Application projects.
- No `AutoMapper` references anywhere.
- No empty catch blocks (via Roslyn analyzer — covered by `TreatWarningsAsErrors`).

### 5. Contract Tests (`tests/ResolveOps.ContractTests/`)

**Scope:** Carrier webhook adapters, integration event serialization, external HTTP adapters.

**Required scenarios (webhook):**
- Valid signature accepted.
- Invalid signature rejected (401/400).
- Expired timestamp rejected.
- Duplicate event ID idempotent.
- Missing optional fields handled gracefully.
- Unknown event code quarantined.
- Malformed JSON rejected.
- Oversized payload rejected.

**Required scenarios (event contract):**
- Known fixtures serialize and deserialize correctly.
- Required fields remain present.
- Optional fields can be added without breaking existing consumers.

### 6. End-to-End Tests (`tests/ResolveOps.EndToEndTests/`)

**Scope:** Full browser journeys via Playwright (Phase 15+).

**Required journeys:**
- Journey A: Delay exception — create shipment → miss pickup → case appears → assign → resolve.
- Journey B: Damage claim — damage event → case → upload evidence → claim → submit → partial approval → credit note → close.
- Journey C: Duplicate and concurrency — same webhook → verify one effect → two users → 409 on stale update.
- Journey D: Tenant isolation — Tenant A creates resources → Tenant B gets 404/403 on all.

### 7. Performance Tests (`tests/ResolveOps.PerformanceTests/`)

**Tool:** k6 or NBomber (choose one; document in ADR).

**Required scenarios (Phase 16):**
1. Tracking event steady ingestion (100 events/s sustained).
2. Tracking burst ingestion (300 events/s for 5 minutes).
3. Duplicate-heavy ingestion (5% duplicate rate).
4. Open-case dashboard under concurrent users.
5. Timeline query for cases with 10,000 entries.
6. Claim deadline scan over large dataset.
7. CSV import.
8. Asynchronous export.
9. Outbox backlog recovery after broker outage.

---

## Coverage Policy

> Coverage percentage is evidence, not the definition of quality.

**Required 100% coverage:**
- All state transition branches (tested in unit test matrix).
- All authorization policies for privileged actions.
- All tenant isolation paths for core resources.
- All idempotency and concurrency scenarios.
- All money calculations and rounding rules.
- All policy version selection rules.

---

## Test Data Rules

- Use deterministic seeds (Bogus library with fixed seeds).
- No production data.
- Vietnamese names/text are included only as synthetic examples.
- Model realistic timezones (Asia/Ho_Chi_Minh, UTC, America/New_York), currencies (VND, USD), and quantities.
- Test builders expose meaningful defaults but require explicit values for important business fields.
- Avoid randomized tests that cannot be reproduced.

---

## State Machine Matrix Tests

Every aggregate with a state machine must have a table-driven test covering:

| Aggregate | Matrix Location |
|---|---|
| ExceptionCase | `tests/ResolveOps.UnitTests/Modules/Exceptions/ExceptionCaseStateMachineTests.cs` |
| Claim | `tests/ResolveOps.UnitTests/Modules/Claims/ClaimStateMachineTests.cs` |
| WorkflowTask | `tests/ResolveOps.UnitTests/Modules/Workflow/WorkflowTaskStateMachineTests.cs` |
| SLAClock | `tests/ResolveOps.UnitTests/Modules/Workflow/SlaClockStateMachineTests.cs` |

The test suite must fail when a new state or command is added without updating the matrix.

---

## CI Test Commands

```bash
# Unit + Architecture tests (fast — no Docker required)
dotnet test tests/ResolveOps.UnitTests/ tests/ResolveOps.ArchitectureTests/ --configuration Release

# Integration tests (requires Docker)
dotnet test tests/ResolveOps.IntegrationTests/ --configuration Release

# Contract tests
dotnet test tests/ResolveOps.ContractTests/ --configuration Release

# All tests
dotnet test ResolveOps.slnx --configuration Release
```

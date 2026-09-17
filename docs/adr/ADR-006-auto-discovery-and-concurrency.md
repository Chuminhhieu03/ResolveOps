# ADR-006: Auto-discovery and Concurrency Stamp

**Date:** 2026-09-10
**Status:** Accepted

## Context
The original specification (v1.0) explicitly prohibited the use of reflection for auto-registering modules/endpoints (Rule 10) and mandated the use of a `long Version` field for optimistic concurrency (Section 15.1).

However, during the implementation phase, the team determined that manual registration of every endpoint and handler in the `PartnersModule` and future modules causes excessive boilerplate and makes the codebase harder to maintain.

Additionally, standardizing optimistic concurrency using a string-based `ConcurrencyStamp` (similar to the ABP framework) provides better consistency and integration with modern architectural patterns.

## Decision
1. **Auto-discovery (Reflection)**: We will override Rule 10 to allow the use of Reflection for auto-registering handlers (`AddHandlersFromAssembly`) and Minimal API endpoints (`MapEndpointsFromAssembly`). All endpoints must implement an `IEndpoint` interface.
2. **Optimistic Concurrency**: We will replace the `long Version` property with a `string ConcurrencyStamp` property across all aggregate roots and entities. This is standardized via an `IHasConcurrencyStamp` interface. Concurrency token comparison and stamp rotation (rolling a new GUID on update, or initializing on insert) are handled centrally and exclusively inside `AppDbContext.ApplyAuditAndConcurrency()` during `SaveChangesAsync()`. Domain entity mutation methods (`Update`, `Triage`, `Assign`, `Resolve`, etc.) MUST NOT manually assign `ConcurrencyStamp`. Handlers assign `entity.ConcurrencyStamp = command.ConcurrencyStamp` to convey the client stamp to EF Core.

## Consequences
- **Positive:** Significant reduction in boilerplate code in module composition roots (`PartnersModule.cs`). Easier integration for new developers familiar with ABP-style concurrency.
- **Negative:** Reflection at startup slightly increases boot time, though this is negligible for the scale of this modular monolith.

## References
- Overrides `LOGISTICS_EXCEPTION_CARRIER_CLAIMS_MASTER_SPEC.md` Section 0.1 Rule 10 and Section 15.1.

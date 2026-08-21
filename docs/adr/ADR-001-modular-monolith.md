# ADR-001 — Modular Monolith Architecture

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

ResolveOps is a logistics exception and carrier claims platform being built by a single developer over 6–12 months.
The core business workflows (exception detection, case lifecycle, claim submission, financial recovery)
require strong transactional consistency — a case cannot be created and its outbox message left uncommitted.

The initial design question is: **microservices or monolith?**

## Decision

Use a **Modular Monolith** for the MVP:

- One deployable API process (`ResolveOps.Api`).
- One deployable Worker process (`ResolveOps.Worker`).
- Both share the same compiled module assemblies.
- Module boundaries are enforced by code structure and architecture tests, not by network or deployment boundaries.
- Integration between modules uses domain events → outbox → RabbitMQ → consumers, not direct in-process method calls that bypass module contracts.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| Microservices | Premature operational complexity: distributed tracing, distributed transactions (saga), service discovery, independent deployments, inter-service auth — all unnecessary for a solo developer MVP. |
| Event sourcing | Audit and immutable events are required, but full event sourcing adds event-stream reconstruction complexity and migration overhead without proportional benefit at this scale. |
| Single-project monolith | Module boundaries would be unenforceable without separation. Modules must expose contracts, not internal EF entities. |

## Consequences

### Positive
- One developer can reason about and deploy the entire system.
- Most business actions achieve transactional consistency through a single SQL Server transaction.
- Module boundaries remain explicit and enforceable via architecture tests.
- Event-driven behavior (outbox → RabbitMQ → consumers) is implemented without distributed deployment.
- The system can later extract high-load or independently governed modules into separate services when measured throughput justifies it.

### Negative / Trade-offs
- A slow or blocking module could affect API response times (mitigated by Worker isolation and bounded channel concurrency).
- Shared deployment means all modules upgrade together; no independent versioning.
- Requires discipline to maintain module boundaries without compiler enforcement.

### Neutral
- Architecture tests will enforce that modules do not directly reference other modules' internal types.

## References

- Master Specification Section 12.1
- Master Specification Section 0 (MUST NOT: Convert the system into microservices)

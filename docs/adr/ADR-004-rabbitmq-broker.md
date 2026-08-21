# ADR-004 — RabbitMQ as Message Broker

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

ResolveOps is an event-driven modular monolith. Modules publish events and other modules consume them asynchronously to perform side effects (e.g., exceptions evaluation, notifications). We need a message broker to decouple modules, provide durability, and handle retries/dead-lettering.

## Decision

Use **RabbitMQ 3.x** as the message broker.

- Client: `RabbitMQ.Client v7`.
- No abstraction layers like MassTransit or NServiceBus (to demonstrate direct understanding of messaging patterns).
- Use **quorum queues** for all durable business queues to ensure data safety.
- Use **manual consumer acknowledgement** (`basicAck`/`basicNack`).
- Topology:
  - Topic exchange `resolveops.domain-events` for integration events.
  - Direct queues for specific workloads (e.g., `resolveops.tracking-ingestion`).
  - Dead-letter exchange `resolveops.dlx` for failed messages.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| Azure Service Bus | Azure Student Credit is exhausted. Avoid proprietary cloud lock-in for MVP. |
| MassTransit / NServiceBus | Adds significant abstraction. Explicit RabbitMQ.Client usage is clearer for demonstrating messaging pattern knowledge in this project. |
| Redis Streams | Redis is used for caching, not as a durable primary message broker. RabbitMQ offers better out-of-the-box DLX and routing features. |
| Kafka | Too heavy and operationally complex for this MVP scale. RabbitMQ is simpler and sufficient for the required throughput. |

## Consequences

### Positive
- Open-source, Docker-native, free for local dev and production.
- Quorum queues provide strong durability.
- Explicit `RabbitMQ.Client` usage keeps dependencies minimal and demonstrates core skills.
- Built-in management UI.

### Negative / Trade-offs
- Managing connections, channels, and retries manually requires careful implementation (handled in Phase 5).
- At-least-once delivery requires consumer idempotency (inbox pattern).

## References

- Master Specification Section 13.2
- Master Specification Section 17 (Event contracts and messaging design)

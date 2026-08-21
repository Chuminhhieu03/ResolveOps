# ADR-003 — Vertical Slice Architecture Without Mandatory Mediator

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

The modular monolith structure (ADR-001) needs an internal code organization pattern for features.
Two common approaches are:
1. **Layered architecture** — horizontal layers: Controllers → Application Services → Domain → Repositories.
2. **Vertical Slice Architecture** — each feature owns its complete slice: endpoint, validation, handler, mapping, and tests.

Additionally, many .NET projects default to using MediatR as a pipeline between controller and handler.
The question is whether to make MediatR mandatory.

## Decision

Use **Vertical Slice Architecture** within each module.

Each feature is organized as a self-contained folder under its module's `Application/Features/` directory:

```
Modules/Claims/Application/Features/SubmitClaim/
  SubmitClaimEndpoint.cs
  SubmitClaimCommand.cs
  SubmitClaimValidator.cs
  SubmitClaimHandler.cs
  SubmitClaimResponse.cs
  SubmitClaimAuthorization.cs
  SubmitClaimTests.cs
```

**MediatR is explicitly not required.** Endpoints resolve feature handlers directly through ASP.NET Core DI:

```csharp
app.MapPost("/claims/{claimId}/submit", async (
    Guid claimId,
    SubmitClaimCommand command,
    SubmitClaimHandler handler,
    CancellationToken ct) =>
{
    var result = await handler.HandleAsync(command, ct);
    return result.Match(/* ... */);
});
```

This avoids adding a mediator solely for ceremony while retaining clear command/query boundaries.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| MediatR (mandatory) | Adds abstraction without measurable benefit for this codebase size. Obscures direct handler dependencies. Makes testing require additional pipeline configuration. The spec explicitly permits direct DI resolution. |
| Classic layered architecture | Forces cross-cutting dependencies between layers for each feature change. Vertical slices are more cohesive per feature and better aligned with the "finish one vertical slice completely" rule. |
| CQRS with separate read/write models from day one | Premature optimization. Read models and projections are introduced in specific reporting phases after write models stabilize. |

## Consequences

### Positive
- Each feature is self-contained — adding a feature does not require changes across multiple layers.
- Tests live alongside the feature code.
- Feature handler dependencies are explicit (constructor injection).
- No pipeline configuration overhead.
- No MediatR abstractions to learn, configure, or debug.

### Negative / Trade-offs
- Cross-cutting concerns (logging, validation pipeline, authorization) must be handled explicitly per handler or via DI decorators/middleware, not via a built-in MediatR pipeline behavior.
- Feature discovery (e.g., automatically registering all endpoints) requires a convention or assembly scanning approach.

### Neutral
- Handlers are registered in `DependencyInjection.cs` per module.
- Minimal API endpoint registration follows the convention in `Endpoints/` per module.

## References

- Master Specification Section 12.4 (Command/query model)
- Master Specification Section 13.8 (explicitly rejected: MassTransit/NServiceBus; MediatR not listed as rejected but not required)
- Master Specification Section 0.1 rule 17: "Finish one vertical slice completely"

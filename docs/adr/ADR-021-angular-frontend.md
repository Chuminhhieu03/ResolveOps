# ADR-021 — Angular for Frontend

- **Date:** 2026-08-21
- **Status:** Accepted

---

## Context

ResolveOps requires a web frontend for operational users to manage shipments, exceptions, claims, and policies. The frontend needs to handle complex forms, data tables, and structured enterprise workflows.

## Decision

Use **Angular 19+** with **TypeScript** and **Angular Material**.

- Strongly typed, consistent with .NET team tooling conventions.
- `RxJS` for server-state fetching, real-time data streams, and SignalR integration.
- `Zod` for client-side schema validation.

## Alternatives Considered

| Alternative | Reason Not Chosen |
|---|---|
| React / Vite | Popular, but Angular is selected for alignment with the .NET ecosystem conventions, enterprise tooling consistency, and built-in dependency injection. |
| Vue.js | Lacks the structured enterprise framework nature of Angular out-of-the-box. |
| ASP.NET Core MVC / Razor Pages | We want a rich Single Page Application (SPA) experience with client-side routing and reactive components, separate from the API. |

## Consequences

### Positive
- Enterprise-grade framework.
- Angular Material provides consistent, accessible UI components.
- Strong typing end-to-end (TypeScript + C#).

### Negative / Trade-offs
- Steeper learning curve compared to some lightweight frameworks.
- Heavier initial bundle size (mitigated by lazy loading).

## References

- Master Specification Section 13.4

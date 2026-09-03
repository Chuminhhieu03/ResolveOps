# ADR 008: Result Pattern for Domain Errors

## Status
Accepted

## Context
In a Vertical Slice Architecture, business logic often encounters validation or state failures (e.g., "User already exists," "Invalid tenant state"). We need a consistent mechanism to propagate these domain errors from the application layer to the API presentation layer without relying on exceptions.
Using exceptions for control flow is a known anti-pattern that impacts performance and makes it harder to reason about business rules.

## Decision
We will use the **Result pattern** (e.g., `Result<T, Error>`) to express success and failure explicitly in the application layer.

For endpoints/presentation, we will map these `Result` objects into ASP.NET Core `ProblemDetails` responses (HTTP 400 Bad Request, HTTP 404 Not Found, HTTP 409 Conflict).

## Consequences
### Positive
- **Explicit contracts**: Handlers clearly declare their potential failures in their return types.
- **Performance**: Avoids the expensive stack-trace generation of `throw new Exception()`.
- **Standardized API errors**: RFC 7807 `ProblemDetails` is a standard way to report errors in HTTP APIs.

### Negative
- **Boilerplate**: Requires writing mapping logic in the API endpoints to convert `Result` objects to HTTP results.
- **Discipline**: Developers must avoid throwing exceptions for expected business rule violations.

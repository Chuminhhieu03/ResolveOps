namespace ResolveOps.Domain;

public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError ValidationFailed = new("VALIDATION_FAILED", "A validation error occurred.");
    public static readonly DomainError ResourceNotFound = new("RESOURCE_NOT_FOUND", "The requested resource was not found.");
    public static readonly DomainError Forbidden = new("FORBIDDEN", "You do not have permission to perform this action.");
    public static readonly DomainError ConcurrencyConflict = new("CONCURRENCY_CONFLICT", "The resource was modified by another user.");
    public static readonly DomainError InvalidStateTransition = new("INVALID_STATE_TRANSITION", "The requested state transition is invalid.");
}

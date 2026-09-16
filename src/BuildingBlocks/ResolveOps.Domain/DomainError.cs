namespace ResolveOps.Domain;

public sealed record DomainError(string Code, string Message = "", params object[] Arguments)
{
    public static DomainError Failure(string code, string message) => new(code, message);
    public static DomainError ResourceNotFound => new("ERR_RESOURCE_NOT_FOUND", "Resource not found.");
    public static DomainError ConcurrencyConflict => new("ERR_CONCURRENCY_CONFLICT", "Concurrency conflict.");
    public static DomainError ValidationFailed => new("ERR_VALIDATION_FAILED", "Validation failed.");
    public static DomainError Forbidden => new("ERR_FORBIDDEN", "Forbidden.");
}

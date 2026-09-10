namespace ResolveOps.Modules.Partners.Features.GetCustomer;

public sealed record CustomerResponse(
    Guid Id,
    string Code,
    string Name,
    string Priority,
    string? DefaultTimezone,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string ConcurrencyStamp);

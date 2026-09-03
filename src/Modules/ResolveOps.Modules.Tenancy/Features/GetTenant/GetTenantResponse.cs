namespace ResolveOps.Modules.Tenancy.Features.GetTenant;

public sealed record GetTenantResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string DefaultTimezone,
    string DefaultCurrency,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

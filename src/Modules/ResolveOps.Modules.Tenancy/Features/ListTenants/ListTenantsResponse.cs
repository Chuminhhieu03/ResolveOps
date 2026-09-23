namespace ResolveOps.Modules.Tenancy.Features.ListTenants;

public sealed record TenantDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string DefaultTimezone,
    string DefaultCurrency);

public sealed record ListTenantsResponse(IReadOnlyList<TenantDto> Tenants);

namespace ResolveOps.Modules.Tenancy.Features.UpdateTenant;

public sealed record UpdateTenantCommand(
    string Name,
    string DefaultTimezone,
    string DefaultCurrency,
    string ConcurrencyStamp);

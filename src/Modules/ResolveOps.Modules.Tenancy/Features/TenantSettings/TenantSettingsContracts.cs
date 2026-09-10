namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

public sealed record GetTenantSettingsQuery();
public sealed record GetTenantSettingsResponse(string SettingsJson, string ConcurrencyStamp);
public sealed record UpdateTenantSettingsCommand(string SettingsJson, string ConcurrencyStamp);

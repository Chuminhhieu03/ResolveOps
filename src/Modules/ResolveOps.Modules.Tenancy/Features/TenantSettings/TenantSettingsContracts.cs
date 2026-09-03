namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

public sealed record GetTenantSettingsQuery();
public sealed record GetTenantSettingsResponse(string SettingsJson, long Version);

public sealed record UpdateTenantSettingsCommand(string SettingsJson, long ExpectedVersion);

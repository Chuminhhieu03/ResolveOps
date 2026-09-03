namespace ResolveOps.Modules.Identity.Features.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    Guid TenantId,
    string? IpAddress,
    string? UserAgent);

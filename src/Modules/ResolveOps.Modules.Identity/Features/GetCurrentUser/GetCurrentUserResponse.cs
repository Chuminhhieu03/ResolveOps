namespace ResolveOps.Modules.Identity.Features.GetCurrentUser;

public sealed record GetCurrentUserResponse(
    Guid UserId,
    string Email,
    Guid TenantId,
    IReadOnlyList<string> Roles);

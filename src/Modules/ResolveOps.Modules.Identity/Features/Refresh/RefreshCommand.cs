namespace ResolveOps.Modules.Identity.Features.Refresh;

/// <summary>
/// Refresh token command. 
/// The Token is extracted from the HttpOnly cookie, and IpAddress/UserAgent from the HttpContext.
/// </summary>
public sealed record RefreshCommand(string Token, string? IpAddress, string? UserAgent);

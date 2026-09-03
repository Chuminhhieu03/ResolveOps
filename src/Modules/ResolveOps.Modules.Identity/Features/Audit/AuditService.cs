using Microsoft.Extensions.Logging;
using ResolveOps.Domain.Identity;

namespace ResolveOps.Modules.Identity.Features.Audit;

#pragma warning disable CA1848, CA1873
/// <summary>
/// Domain service for logging authentication and authorization events (Phase 2).
/// In Phase 5, this will be replaced by an outbox-based durable audit log.
/// For now, we use structured Serilog logging so Seq can ingest the events.
/// Never logs sensitive tokens or passwords.
/// </summary>
public sealed class AuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public void LogLoginSuccess(Guid tenantId, Guid userId, string email, string? ipAddress)
    {
        _logger.LogInformation(
            "Authentication successful for user {Email} ({UserId}) in tenant {TenantId} from IP {IpAddress}",
            email, userId, tenantId, ipAddress);
    }

    public void LogLoginFailure(string email, string reason, string? ipAddress)
    {
        _logger.LogWarning(
            "Authentication failed for {Email}. Reason: {Reason}. IP: {IpAddress}",
            email, reason, ipAddress);
    }

    public void LogTokenRefresh(Guid tenantId, Guid userId, Guid familyId)
    {
        _logger.LogInformation(
            "Token rotated for user {UserId} in tenant {TenantId} (Family {FamilyId})",
            userId, tenantId, familyId);
    }

    public void LogTokenReuseDetected(Guid tenantId, Guid userId, Guid familyId)
    {
        _logger.LogWarning(
            "Token reuse detected for user {UserId} in tenant {TenantId} (Family {FamilyId}). Entire family revoked.",
            userId, tenantId, familyId);
    }

    public void LogLogout(Guid tenantId, Guid userId, Guid familyId)
    {
        _logger.LogInformation(
            "Session revoked (logout) for user {UserId} in tenant {TenantId} (Family {FamilyId})",
            userId, tenantId, familyId);
    }

    public void LogPasswordChanged(Guid userId)
    {
        _logger.LogInformation("Password changed for user {UserId}", userId);
    }
}

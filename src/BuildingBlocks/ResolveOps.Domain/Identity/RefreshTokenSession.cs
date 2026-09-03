namespace ResolveOps.Domain.Identity;

/// <summary>
/// Persisted refresh token session enabling rotating refresh tokens with reuse detection.
///
/// Security model (spec §15.3):
/// - Only the SHA-256 hash of the token is stored — never the plaintext token.
/// - Token families allow full-family revocation on reuse detection.
/// - Each successful refresh replaces the used token and creates a new one.
/// - If a revoked token is reused, the entire family is revoked (prevents replay after theft).
/// - IP and user-agent are stored as hashes for anomaly detection without storing PII.
///
/// Concurrency: version column prevents lost-update on simultaneous refresh attempts.
/// </summary>
public sealed class RefreshTokenSession
{
    public Guid Id { get; private set; }

    /// <summary>Tenant scope for this session. Used for cache key and audit isolation.</summary>
    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the opaque refresh token. Never store plaintext.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>All sessions in the same family are revoked together on reuse detection.</summary>
    public Guid FamilyId { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Points to the session that replaced this one after a successful rotation.</summary>
    public Guid? ReplacedById { get; private set; }

    /// <summary>Hash of the client IP address. Null if unavailable.</summary>
    public string? CreatedIpHash { get; private set; }

    /// <summary>Hash of the User-Agent string. Null if unavailable.</summary>
    public string? UserAgentHash { get; private set; }

    /// <summary>Optimistic concurrency token (spec §15.1).</summary>
    public long Version { get; private set; }

    // EF Core requires a parameterless constructor.
    private RefreshTokenSession() { }

    /// <summary>Creates a new session as the start of a new token family.</summary>
    public static RefreshTokenSession CreateNew(
        Guid tenantId,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        TimeProvider timeProvider,
        string? ipHash = null,
        string? userAgentHash = null)
    {
        return new RefreshTokenSession
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = Guid.CreateVersion7(),
            IssuedAtUtc = timeProvider.GetUtcNow(),
            ExpiresAtUtc = expiresAtUtc,
            CreatedIpHash = ipHash,
            UserAgentHash = userAgentHash,
            Version = 1,
        };
    }

    /// <summary>Creates a replacement session in the same family after a successful rotation.</summary>
    public static RefreshTokenSession CreateRotated(
        RefreshTokenSession previous,
        string newTokenHash,
        DateTimeOffset expiresAtUtc,
        TimeProvider timeProvider,
        string? ipHash = null,
        string? userAgentHash = null)
    {
        return new RefreshTokenSession
        {
            Id = Guid.CreateVersion7(),
            TenantId = previous.TenantId,
            UserId = previous.UserId,
            TokenHash = newTokenHash,
            FamilyId = previous.FamilyId,
            IssuedAtUtc = timeProvider.GetUtcNow(),
            ExpiresAtUtc = expiresAtUtc,
            CreatedIpHash = ipHash,
            UserAgentHash = userAgentHash,
            Version = 1,
        };
    }

    /// <summary>
    /// Returns true when the token has expired or has been revoked.
    /// Callers must check this before issuing new tokens.
    /// </summary>
    public bool IsExpired(TimeProvider timeProvider) =>
        ExpiresAtUtc <= timeProvider.GetUtcNow();

    public bool IsRevoked => RevokedAtUtc.HasValue;

    /// <summary>Marks this session as revoked and records which session replaced it.</summary>
    public void Revoke(TimeProvider timeProvider, Guid? replacedById = null)
    {
        RevokedAtUtc = timeProvider.GetUtcNow();
        ReplacedById = replacedById;
    }
}

namespace ResolveOps.Domain.Messaging;

/// <summary>
/// API-level idempotency record (spec §15.11).
///
/// Used by endpoints that accept an Idempotency-Key header (e.g. POST /shipments).
/// The first request with a given key stores the response; subsequent requests with
/// the same key return the stored response without re-executing the command.
///
/// If the same key is reused with a different request hash, 409 Conflict is returned.
///
/// Primary key is composite: (TenantId, Scope, IdempotencyKey).
/// </summary>
public sealed class IdempotencyRecord
{
    public Guid TenantId { get; private set; }

    /// <summary>Logical scope, e.g. "CreateShipment", "CreateClaim".</summary>
    public string Scope { get; private set; } = string.Empty;

    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>SHA-256 hash of the canonical request body to detect key reuse with different content.</summary>
    public string RequestHash { get; private set; } = string.Empty;

    public int ResponseStatus { get; private set; }

    /// <summary>JSON-serialized response body stored for replay.</summary>
    public string? ResponseBody { get; private set; }

    /// <summary>ID of the resource created by the original request. Null for non-create commands.</summary>
    public Guid? ResourceId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    // EF Core requires a parameterless constructor.
    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(
        Guid tenantId,
        string scope,
        string idempotencyKey,
        string requestHash,
        int responseStatus,
        string? responseBody,
        Guid? resourceId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        return new IdempotencyRecord
        {
            TenantId = tenantId,
            Scope = scope,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash,
            ResponseStatus = responseStatus,
            ResponseBody = responseBody,
            ResourceId = resourceId,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc,
        };
    }
}

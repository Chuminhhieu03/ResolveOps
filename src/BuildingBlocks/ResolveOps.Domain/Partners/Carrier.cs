namespace ResolveOps.Domain.Partners;

/// <summary>
/// Carrier aggregate root (spec §15.4).
///
/// Invariants:
/// - Code is unique within a tenant (UNIQUE INDEX on tenant_id, code).
/// - An inactive carrier cannot be assigned to new shipment legs.
/// - Version is used for optimistic concurrency.
/// </summary>
public sealed class Carrier
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Stable short identifier, e.g. "GHTK". Unique within tenant.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Active or Inactive.</summary>
    public string Status { get; private set; } = CarrierStatus.Active;

    /// <summary>SCAC code or carrier-specific external identifier. Optional.</summary>
    public string? ScacOrExternalCode { get; private set; }

    /// <summary>IANA timezone for this carrier's operational base. Optional.</summary>
    public string? DefaultTimezone { get; private set; }

    /// <summary>Primary claims contact email. Optional.</summary>
    public string? ContactEmail { get; private set; }

    /// <summary>
    /// How claims are submitted to this carrier: Manual, Email, Portal, API.
    /// </summary>
    public string ClaimSubmissionChannel { get; private set; } = ResolveOps.Domain.Partners.ClaimSubmissionChannel.Manual;

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (spec §15.1).</summary>
    public long Version { get; private set; }

    // EF Core requires a parameterless constructor.
    private Carrier() { }

    public static Carrier Create(
        Guid tenantId,
        string code,
        string name,
        string? scacOrExternalCode,
        string? defaultTimezone,
        string? contactEmail,
        string claimSubmissionChannel,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new Carrier
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Status = CarrierStatus.Active,
            ScacOrExternalCode = scacOrExternalCode?.Trim().ToUpperInvariant(),
            DefaultTimezone = defaultTimezone,
            ContactEmail = contactEmail?.Trim(),
            ClaimSubmissionChannel = claimSubmissionChannel,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };
    }

    public void Update(
        string name,
        string? scacOrExternalCode,
        string? defaultTimezone,
        string? contactEmail,
        string claimSubmissionChannel,
        TimeProvider timeProvider)
    {
        Name = name.Trim();
        ScacOrExternalCode = scacOrExternalCode?.Trim().ToUpperInvariant();
        DefaultTimezone = defaultTimezone;
        ContactEmail = contactEmail?.Trim();
        ClaimSubmissionChannel = claimSubmissionChannel;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }

    /// <summary>
    /// Activates the carrier so it can be assigned to shipment legs.
    /// </summary>
    public void Activate(TimeProvider timeProvider)
    {
        Status = CarrierStatus.Active;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }

    /// <summary>
    /// Deactivates the carrier. An inactive carrier cannot be assigned to new shipment legs.
    /// Existing legs are not affected (spec §24 Phase 3 DoD).
    /// </summary>
    public void Deactivate(TimeProvider timeProvider)
    {
        Status = CarrierStatus.Inactive;
        UpdatedAtUtc = timeProvider.GetUtcNow();
        Version++;
    }

    public bool IsActive => Status == CarrierStatus.Active;
}

/// <summary>Valid values for <see cref="Carrier.Status"/>.</summary>
public static class CarrierStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

/// <summary>Valid values for <see cref="Carrier.ClaimSubmissionChannel"/>.</summary>
public static class ClaimSubmissionChannel
{
    public const string Manual = "Manual";
    public const string Email = "Email";
    public const string Portal = "Portal";
    public const string Api = "API";

    public static readonly IReadOnlyList<string> All = [Manual, Email, Portal, Api];

    public static bool IsValid(string value) =>
        All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

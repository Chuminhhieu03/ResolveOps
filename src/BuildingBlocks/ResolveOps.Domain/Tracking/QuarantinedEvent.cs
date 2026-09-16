namespace ResolveOps.Domain.Tracking;

/// <summary>
/// Quarantined tracking event that could not be automatically matched or normalized (spec §15.6).
///
/// Can be resolved by linking to a shipment or reprocessed once missing reference data is populated.
/// Uses string ConcurrencyStamp for optimistic concurrency (ADR-006).
/// </summary>
public sealed class QuarantinedEvent : IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid InboundReceiptId { get; private set; }
    public string ReasonCode { get; private set; } = string.Empty;
    public string Detail { get; private set; } = string.Empty;
    public string Status { get; private set; } = QuarantinedEventStatus.Quarantined;
    public Guid? AssignedUserId { get; private set; }
    public Guid? ResolvedShipmentId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Optimistic concurrency token (ADR-006).</summary>
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    // EF Core requires a parameterless constructor
    private QuarantinedEvent() { }

    public static QuarantinedEvent Create(
        Guid tenantId,
        Guid inboundReceiptId,
        string reasonCode,
        string detail,
        TimeProvider timeProvider)
    {
        return new QuarantinedEvent
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            InboundReceiptId = inboundReceiptId,
            ReasonCode = reasonCode.Trim(),
            Detail = detail.Trim(),
            Status = QuarantinedEventStatus.Quarantined,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }

    public void Resolve(Guid shipmentId, Guid? assignedUserId, TimeProvider timeProvider)
    {
        Status = QuarantinedEventStatus.Resolved;
        ResolvedShipmentId = shipmentId;
        AssignedUserId = assignedUserId;
        ResolvedAtUtc = timeProvider.GetUtcNow();
    }

    public void MarkReprocessed(TimeProvider timeProvider)
    {
        Status = QuarantinedEventStatus.Reprocessed;
        ResolvedAtUtc = timeProvider.GetUtcNow();
    }

    public void Ignore(Guid? assignedUserId, TimeProvider timeProvider)
    {
        Status = QuarantinedEventStatus.Ignored;
        AssignedUserId = assignedUserId;
        ResolvedAtUtc = timeProvider.GetUtcNow();
    }
}

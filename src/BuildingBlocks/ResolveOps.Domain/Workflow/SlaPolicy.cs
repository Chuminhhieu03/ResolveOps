namespace ResolveOps.Domain.Workflow;

/// <summary>
/// SLA policy definition aggregate root (spec §15.8).
/// </summary>
public sealed class SlaPolicy : IAuditableEntity, IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string PolicyKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    // IHasConcurrencyStamp
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private SlaPolicy() { }

    public static SlaPolicy Create(
        Guid tenantId,
        string policyKey,
        string name,
        string? description,
        TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new SlaPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyKey = policyKey.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };
    }
}

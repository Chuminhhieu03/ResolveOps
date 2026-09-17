namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Audit trail entry for case lifecycle events, transitions, and notes (spec §15.7).
/// </summary>
public sealed class CaseTimelineEntry
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public string EntryType { get; private set; } = string.Empty;
    public string ActorType { get; private set; } = string.Empty;
    public Guid? ActorId { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? DetailsJson { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }

    private CaseTimelineEntry() { }

    public static CaseTimelineEntry Create(
        Guid tenantId,
        Guid caseId,
        string entryType,
        string actorType,
        Guid? actorId,
        string summary,
        string? detailsJson,
        string? correlationId,
        TimeProvider timeProvider)
    {
        return new CaseTimelineEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            EntryType = entryType,
            ActorType = actorType,
            ActorId = actorId,
            Summary = summary,
            DetailsJson = detailsJson,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            CorrelationId = correlationId
        };
    }
}

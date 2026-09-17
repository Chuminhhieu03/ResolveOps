namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Child entity attaching repeated or subsequent related tracking signals to an active case (spec §15.7).
/// </summary>
public sealed class ExceptionOccurrence
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid? TrackingEventId { get; private set; }
    public string OccurrenceType { get; private set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private ExceptionOccurrence() { }

    public static ExceptionOccurrence Create(
        Guid tenantId,
        Guid caseId,
        Guid? trackingEventId,
        string occurrenceType,
        DateTimeOffset observedAtUtc,
        string summary,
        TimeProvider timeProvider)
    {
        return new ExceptionOccurrence
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CaseId = caseId,
            TrackingEventId = trackingEventId,
            OccurrenceType = occurrenceType,
            ObservedAtUtc = observedAtUtc,
            Summary = summary,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };
    }
}

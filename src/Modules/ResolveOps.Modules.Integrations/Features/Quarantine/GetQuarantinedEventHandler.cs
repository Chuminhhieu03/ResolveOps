using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

internal sealed class GetQuarantinedEventHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetQuarantinedEventHandler(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<QuarantinedEventDetailDto>> HandleAsync(
        Guid quarantinedEventId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var quarantinedEvent = await _dbContext.QuarantinedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quarantinedEventId && q.TenantId == tenantId, cancellationToken);

        if (quarantinedEvent is null)
        {
            return DomainError.Failure(
                "ERR_QUARANTINED_EVENT_NOT_FOUND",
                $"Quarantined event with ID '{quarantinedEventId}' was not found.");
        }

        var receipt = await _dbContext.InboundEventReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == quarantinedEvent.InboundReceiptId && r.TenantId == tenantId, cancellationToken);

        return new QuarantinedEventDetailDto(
            quarantinedEvent.Id,
            quarantinedEvent.InboundReceiptId,
            quarantinedEvent.ReasonCode,
            quarantinedEvent.Detail,
            quarantinedEvent.Status,
            quarantinedEvent.AssignedUserId,
            quarantinedEvent.ResolvedShipmentId,
            quarantinedEvent.ResolvedAtUtc,
            quarantinedEvent.CreatedAtUtc,
            receipt?.SourceSystem ?? string.Empty,
            receipt?.ExternalEventId,
            receipt?.RawPayload ?? string.Empty,
            receipt?.ReceivedAtUtc ?? DateTimeOffset.MinValue);
    }
}

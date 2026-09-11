using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Integrations.Features.Quarantine;

internal sealed class ListQuarantinedEventsHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public ListQuarantinedEventsHandler(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ListQuarantinedEventsResponse>> HandleAsync(
        string? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var query = _dbContext.QuarantinedEvents
            .AsNoTracking()
            .Where(q => q.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim();
            query = query.Where(q => q.Status == normalizedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(q => q.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuarantinedEventSummaryDto(
                q.Id,
                q.InboundReceiptId,
                q.ReasonCode,
                q.Detail,
                q.Status,
                q.AssignedUserId,
                q.ResolvedShipmentId,
                q.ResolvedAtUtc,
                q.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new ListQuarantinedEventsResponse(items, totalCount, pageNumber, pageSize);
    }
}

using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Shipments.Features.ListShipments;

internal sealed class ListShipmentsHandler
{
    private readonly AppDbContext _dbContext;

    public ListShipmentsHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListShipmentsResponse>> HandleAsync(
        ListShipmentsQuery query,
        CancellationToken cancellationToken)
    {
        // Clamp page size per spec §16.1 (max 100)
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);

        // Tenant filter applied automatically; do NOT load full event history.
        var dbQuery = _dbContext.Shipments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.StatusFilter))
        {
            dbQuery = dbQuery.Where(s => s.Status == query.StatusFilter);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        // Projection into summary — does NOT load Legs/Items collections (spec §24 Phase 4 DoD)
        var items = await dbQuery
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShipmentSummaryResponse(
                s.Id,
                s.ExternalReference,
                s.SourceSystem,
                s.CustomerId,
                s.OriginLocationId,
                s.DestinationLocationId,
                s.Status,
                s.PlannedPickupAtUtc,
                s.PlannedDeliveryAtUtc,
                s.ActualDeliveryAtUtc,
                _dbContext.ShipmentLegs.Count(l => l.ShipmentId == s.Id),
                s.CreatedAtUtc,
                s.ConcurrencyStamp))
            .ToListAsync(cancellationToken);

        return new ListShipmentsResponse(items, totalCount, page, pageSize);
    }
}

using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Modules.Partners.Features.GetCarrier;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Partners.Features.ListCarriers;

internal sealed class ListCarriersHandler
{
    private readonly AppDbContext _dbContext;

    public ListCarriersHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListCarriersResponse>> HandleAsync(
        ListCarriersQuery query,
        CancellationToken cancellationToken)
    {
        // Clamp page size to spec §16.1 max 100
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var page = Math.Max(1, query.Page);

        var dbQuery = _dbContext.Carriers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.StatusFilter))
        {
            dbQuery = dbQuery.Where(c => c.Status == query.StatusFilter);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderBy(c => c.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CarrierResponse(
                c.Id,
                c.Code,
                c.Name,
                c.Status,
                c.ScacOrExternalCode,
                c.DefaultTimezone,
                c.ContactEmail,
                c.ClaimSubmissionChannel,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                c.ConcurrencyStamp))
            .ToListAsync(cancellationToken);

        return new ListCarriersResponse(items, totalCount, page, pageSize);
    }
}

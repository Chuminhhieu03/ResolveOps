using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Cases.ListExceptionCases;

internal sealed class ListExceptionCasesHandler
{
    private readonly AppDbContext _dbContext;

    public ListExceptionCasesHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListExceptionCasesResponse>> HandleAsync(
        ListExceptionCasesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _dbContext.ExceptionCases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(c => c.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            dbQuery = dbQuery.Where(c => c.Severity == query.Severity);
        }

        if (!string.IsNullOrWhiteSpace(query.ExceptionType))
        {
            dbQuery = dbQuery.Where(c => c.ExceptionType == query.ExceptionType);
        }

        if (query.ShipmentId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.ShipmentId == query.ShipmentId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.DetectedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.DetectedAtUtc <= query.ToUtc.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(c => c.DetectedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ExceptionCaseSummaryResponse(
                c.Id,
                c.CaseNumber,
                c.ShipmentId,
                c.ShipmentLegId,
                c.ExceptionType,
                c.Status,
                c.Severity,
                c.SeverityScore,
                c.OwnerTeamCode,
                c.FinancialExposure,
                c.ExposureCurrency,
                c.DetectedAtUtc,
                c.CreatedAtUtc,
                c.ConcurrencyStamp))
            .ToListAsync(cancellationToken);

        return Result<ListExceptionCasesResponse>.Success(new ListExceptionCasesResponse(
            items,
            totalCount,
            page,
            pageSize));
    }
}

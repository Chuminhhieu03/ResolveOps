using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Policies.ListPolicies;

internal sealed class ListPoliciesHandler
{
    private readonly AppDbContext _dbContext;

    public ListPoliciesHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ListPoliciesResponse>> HandleAsync(
        ListPoliciesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _dbContext.ExceptionPolicies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.ExceptionType))
        {
            dbQuery = dbQuery.Where(p => p.ExceptionType == query.ExceptionType);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(p => p.Status == query.Status);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PolicySummaryResponse(
                p.Id,
                p.PolicyKey,
                p.VersionNumber,
                p.ExceptionType,
                p.Status,
                p.EffectiveFromUtc,
                p.EffectiveToUtc,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result<ListPoliciesResponse>.Success(new ListPoliciesResponse(
            items,
            totalCount,
            page,
            pageSize));
    }
}

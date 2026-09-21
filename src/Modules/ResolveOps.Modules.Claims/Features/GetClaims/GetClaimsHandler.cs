using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.GetClaims;

public class GetClaimsHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetClaimsHandler(DbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<GetClaimsResponse> HandleAsync(
        GetClaimsQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.Set<Claim>()
            .AsNoTracking();

        if (query.CaseId.HasValue)
        {
            dbQuery = dbQuery.Where(c => c.CaseId == query.CaseId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            dbQuery = dbQuery.Where(c => c.Status == query.Status);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var claims = await dbQuery
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ClaimSummaryResponse(
                c.Id,
                c.ClaimNumber,
                c.CaseId,
                c.CarrierId,
                c.ClaimType.ToString(),
                c.Status.ToString(),
                c.ClaimedAmount,
                c.Currency,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new GetClaimsResponse(
            claims.AsReadOnly(),
            totalCount,
            query.Page,
            query.PageSize);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.RemoveLossComponent;

public class RemoveLossComponentHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public RemoveLossComponentHandler(DbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result> HandleAsync(
        RemoveLossComponentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .Include(c => c.LossComponents)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId && c.TenantId == tenantId, cancellationToken);

        if (claim == null)
        {
            return Result.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var result = claim.RemoveLossComponent(command.ComponentId);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

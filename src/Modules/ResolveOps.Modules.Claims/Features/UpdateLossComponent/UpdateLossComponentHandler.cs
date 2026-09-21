using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Claims.ValueObjects;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.UpdateLossComponent;

public class UpdateLossComponentHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public UpdateLossComponentHandler(DbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result> HandleAsync(
        UpdateLossComponentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .Include(c => c.LossComponents)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var money = new Money(command.Amount, command.Currency);

        var result = claim.UpdateLossComponent(
            command.ComponentId,
            command.Description,
            command.Quantity,
            command.UnitAmount,
            money,
            command.SourceDocumentId);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

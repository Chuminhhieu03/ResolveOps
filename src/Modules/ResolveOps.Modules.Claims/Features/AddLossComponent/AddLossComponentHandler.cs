using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Claims;
using ResolveOps.Domain.Claims.Enums;
using ResolveOps.Domain.Claims.ValueObjects;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Tenancy;
using ResolveOps.Security;

namespace ResolveOps.Modules.Claims.Features.AddLossComponent;

public class AddLossComponentHandler
{
    private readonly DbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public AddLossComponentHandler(DbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> HandleAsync(
        AddLossComponentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var claim = await _dbContext.Set<Claim>()
            .Include(c => c.LossComponents)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId && c.TenantId == tenantId, cancellationToken);

        if (claim == null)
        {
            return Result<Guid>.Failure(new DomainError("CLAIM_NOT_FOUND", "Claim not found."));
        }

        var componentType = Enum.Parse<LossComponentType>(command.ComponentType);
        var money = new Money(command.Amount, command.Currency);

        var result = claim.AddLossComponent(
            componentType,
            command.Description,
            command.Quantity,
            command.UnitAmount,
            money,
            command.SourceDocumentId);

        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(result.Value.Id);
    }
}

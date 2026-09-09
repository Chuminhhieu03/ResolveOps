using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Partners;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.CreateCarrier;

internal sealed class CreateCarrierHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreateCarrierHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateCarrierResponse>> HandleAsync(
        CreateCarrierCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var normalizedCode = command.Code.Trim().ToUpperInvariant();

        // spec §15.4 — code unique within tenant
        var codeExists = await _dbContext.Carriers
            .AsNoTracking()
            .AnyAsync(c => c.TenantId == tenantId && c.Code == normalizedCode,
                cancellationToken);

        if (codeExists)
        {
            return DomainError.ValidationFailed with
            {
                Message = $"A carrier with code '{command.Code}' already exists in this tenant."
            };
        }

        var carrier = Carrier.Create(
            tenantId,
            command.Code,
            command.Name,
            command.ScacOrExternalCode,
            command.DefaultTimezone,
            command.ContactEmail,
            command.ClaimSubmissionChannel,
            _timeProvider);

        _dbContext.Carriers.Add(carrier);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateCarrierResponse(carrier.Id);
    }
}

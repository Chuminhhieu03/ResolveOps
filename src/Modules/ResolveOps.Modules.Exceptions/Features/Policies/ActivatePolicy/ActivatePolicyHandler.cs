using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Policies.ActivatePolicy;

internal sealed class ActivatePolicyHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ActivatePolicyHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        ActivatePolicyCommand command,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var policy = await _dbContext.ExceptionPolicies
            .FirstOrDefaultAsync(p => p.Id == command.PolicyId, cancellationToken);

        if (policy is null)
        {
            return DomainError.ResourceNotFound;
        }

        var activateResult = policy.Activate(now);
        if (!activateResult.IsSuccess)
        {
            return activateResult;
        }

        // Retire previously active versions for the same policy key/type
        var previouslyActive = await _dbContext.ExceptionPolicies
            .Where(p => p.TenantId == policy.TenantId &&
                        p.PolicyKey == policy.PolicyKey &&
                        p.Id != policy.Id &&
                        p.Status == PolicyStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var prev in previouslyActive)
        {
            prev.Retire(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

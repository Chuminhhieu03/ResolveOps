using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Policies.RetirePolicy;

internal sealed class RetirePolicyHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RetirePolicyHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        RetirePolicyCommand command,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var policy = await _dbContext.ExceptionPolicies
            .FirstOrDefaultAsync(p => p.Id == command.PolicyId, cancellationToken);

        if (policy is null)
        {
            return DomainError.ResourceNotFound;
        }

        var result = policy.Retire(now);
        if (!result.IsSuccess)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

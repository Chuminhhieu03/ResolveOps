using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Policies.GetActivePolicy;

internal sealed class GetActivePolicyHandler
{
    private readonly AppDbContext _dbContext;

    public GetActivePolicyHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ActivePolicyResponse>> HandleAsync(
        GetActivePolicyQuery query,
        CancellationToken cancellationToken)
    {
        var policy = await _dbContext.ExceptionPolicies
            .AsNoTracking()
            .Where(p => p.ExceptionType == query.ExceptionType &&
                        p.Status == PolicyStatus.Active)
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            return DomainError.Failure(
                "ERR_ACTIVE_POLICY_NOT_FOUND",
                $"No active policy found for exception type '{query.ExceptionType}'.");
        }

        return Result<ActivePolicyResponse>.Success(new ActivePolicyResponse(
            policy.Id,
            policy.PolicyKey,
            policy.VersionNumber,
            policy.ExceptionType,
            policy.Status,
            policy.EffectiveFromUtc,
            policy.RuleDefinitionJson,
            policy.SeverityDefinitionJson,
            policy.AssignmentDefinitionJson,
            policy.EvidencePolicyVersionId,
            policy.SlaPolicyVersionId));
    }
}

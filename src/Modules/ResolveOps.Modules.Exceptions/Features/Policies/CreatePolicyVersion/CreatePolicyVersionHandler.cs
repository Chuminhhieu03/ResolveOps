using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Policies.CreatePolicyVersion;

internal sealed class CreatePolicyVersionHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreatePolicyVersionHandler(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreatePolicyVersionResponse>> HandleAsync(
        CreatePolicyVersionCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var now = _timeProvider.GetUtcNow();

        // 1. Check unique (tenant_id, policy_key, version_number)
        var versionExists = await _dbContext.ExceptionPolicies
            .AnyAsync(
                p => p.TenantId == tenantId &&
                     p.PolicyKey == command.PolicyKey &&
                     p.VersionNumber == command.VersionNumber,
                cancellationToken);

        if (versionExists)
        {
            return DomainError.Failure(
                "ERR_POLICY_VERSION_EXISTS",
                $"Policy '{command.PolicyKey}' with version {command.VersionNumber} already exists for this tenant.");
        }

        var policy = ExceptionPolicy.Create(
            tenantId,
            command.PolicyKey,
            command.VersionNumber,
            command.ExceptionType,
            command.RuleDefinitionJson,
            command.SeverityDefinitionJson,
            command.AssignmentDefinitionJson,
            createdByUserId: Guid.Empty,
            effectiveFromUtc: command.EffectiveFromUtc,
            effectiveToUtc: command.EffectiveToUtc,
            evidencePolicyVersionId: command.EvidencePolicyVersionId,
            slaPolicyVersionId: command.SlaPolicyVersionId,
            status: command.ActivateNow ? PolicyStatus.Active : PolicyStatus.Draft);

        if (command.ActivateNow)
        {
            // Retire previous active policy for this type/key if any
            var previousActive = await _dbContext.ExceptionPolicies
                .Where(p => p.TenantId == tenantId &&
                            p.PolicyKey == command.PolicyKey &&
                            p.Status == PolicyStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var prev in previousActive)
            {
                prev.Retire(now);
            }
        }

        _dbContext.ExceptionPolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreatePolicyVersionResponse>.Success(new CreatePolicyVersionResponse(
            policy.Id,
            policy.PolicyKey,
            policy.VersionNumber,
            policy.ExceptionType,
            policy.Status,
            policy.EffectiveFromUtc,
            policy.EffectiveToUtc));
    }
}

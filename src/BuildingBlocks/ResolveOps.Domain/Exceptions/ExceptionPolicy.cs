namespace ResolveOps.Domain.Exceptions;

/// <summary>
/// Versioned policy configuration governing exception detection, severity calculation, and assignment (spec §15.7).
///
/// Invariants:
/// - (tenant_id, policy_key, version_number) is unique.
/// - Once activated, a policy version is immutable; updates create a new version (spec §16.11, §24 Phase 7).
/// - An active policy version can be retired by setting effective_to_utc.
/// </summary>
public sealed class ExceptionPolicy : IAuditableEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string PolicyKey { get; private set; } = string.Empty;
    public int VersionNumber { get; private set; }
    public string ExceptionType { get; private set; } = string.Empty;
    public string Status { get; private set; } = PolicyStatus.Draft;
    public DateTimeOffset EffectiveFromUtc { get; private set; }
    public DateTimeOffset? EffectiveToUtc { get; private set; }

    /// <summary>Controlled JSON schema defining rules (e.g. tolerance minutes, condition matchers).</summary>
    public string RuleDefinitionJson { get; private set; } = string.Empty;

    /// <summary>Controlled JSON schema defining severity factors and score weights.</summary>
    public string SeverityDefinitionJson { get; private set; } = string.Empty;

    /// <summary>Controlled JSON schema defining default team/queue assignment.</summary>
    public string AssignmentDefinitionJson { get; private set; } = string.Empty;

    public Guid? EvidencePolicyVersionId { get; private set; }
    public Guid? SlaPolicyVersionId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private ExceptionPolicy() { }

    public static ExceptionPolicy Create(
        Guid tenantId,
        string policyKey,
        int versionNumber,
        string exceptionType,
        string ruleDefinitionJson,
        string severityDefinitionJson,
        string assignmentDefinitionJson,
        Guid createdByUserId,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? effectiveToUtc,
        Guid? evidencePolicyVersionId,
        Guid? slaPolicyVersionId,
        string status = PolicyStatus.Draft)
    {
        return new ExceptionPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PolicyKey = policyKey.Trim(),
            VersionNumber = versionNumber,
            ExceptionType = exceptionType.Trim(),
            Status = status,
            EffectiveFromUtc = effectiveFromUtc,
            EffectiveToUtc = effectiveToUtc,
            RuleDefinitionJson = ruleDefinitionJson,
            SeverityDefinitionJson = severityDefinitionJson,
            AssignmentDefinitionJson = assignmentDefinitionJson,
            EvidencePolicyVersionId = evidencePolicyVersionId,
            SlaPolicyVersionId = slaPolicyVersionId,
            CreatedByUserId = createdByUserId
        };
    }

    public Result Activate(DateTimeOffset now)
    {
        if (Status == PolicyStatus.Retired)
        {
            return DomainError.Failure("ERR_POLICY_ALREADY_RETIRED", "A retired policy version cannot be activated. Create a new version instead.");
        }

        Status = PolicyStatus.Active;
        if (EffectiveFromUtc > now)
        {
            EffectiveFromUtc = now;
        }

        EffectiveToUtc = null;
        return Result.Success();
    }

    public Result Retire(DateTimeOffset now)
    {
        if (Status == PolicyStatus.Retired)
        {
            return Result.Success();
        }

        Status = PolicyStatus.Retired;
        EffectiveToUtc = now;
        return Result.Success();
    }
}

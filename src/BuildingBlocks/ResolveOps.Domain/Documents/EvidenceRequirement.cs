namespace ResolveOps.Domain.Documents;

/// <summary>
/// Evidence requirement configuration entity (spec §8.6, §15.9).
///
/// Represents a policy-driven requirement for a specific evidence type on a
/// given exception/claim type. This entity is immutable after creation —
/// changes are managed through policy versioning (new records per version).
///
/// Assumption A-022: No ConcurrencyStamp or IAuditableEntity needed here because
/// EvidenceRequirement records are write-once configuration data aligned to
/// policy versions (consistent with ExceptionPolicy pattern).
/// </summary>
public sealed class EvidenceRequirement
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>The policy version this requirement belongs to.</summary>
    public Guid PolicyVersionId { get; private set; }

    /// <summary>Exception type this requirement applies to (from ExceptionType constants).</summary>
    public string ExceptionType { get; private set; } = string.Empty;

    /// <summary>Optional: claim type this requirement applies to. Null means all claim types.</summary>
    public string? ClaimType { get; private set; }

    /// <summary>Required evidence type from EvidenceType constants.</summary>
    public string EvidenceType { get; private set; } = string.Empty;

    /// <summary>When true, absence of Available evidence of this type blocks claim readiness.</summary>
    public bool IsMandatory { get; private set; }

    /// <summary>
    /// Optional JSON condition expression that gates when this requirement applies.
    /// Evaluated deterministically; not arbitrary code.
    /// </summary>
    public string? ConditionJson { get; private set; }

    private EvidenceRequirement() { }

    public static EvidenceRequirement Create(
        Guid tenantId,
        Guid policyVersionId,
        string exceptionType,
        string? claimType,
        string evidenceType,
        bool isMandatory,
        string? conditionJson = null)
    {
        if (string.IsNullOrWhiteSpace(exceptionType))
        {
            throw new ArgumentException("Exception type is required.", nameof(exceptionType));
        }

        if (!Documents.EvidenceType.IsValid(evidenceType))
        {
            throw new ArgumentException($"Unknown evidence type '{evidenceType}'.", nameof(evidenceType));
        }

        return new EvidenceRequirement
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            PolicyVersionId = policyVersionId,
            ExceptionType = exceptionType.Trim(),
            ClaimType = claimType?.Trim(),
            EvidenceType = evidenceType,
            IsMandatory = isMandatory,
            ConditionJson = conditionJson,
        };
    }
}

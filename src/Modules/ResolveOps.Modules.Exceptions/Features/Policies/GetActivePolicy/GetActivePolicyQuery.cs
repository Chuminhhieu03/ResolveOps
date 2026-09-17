namespace ResolveOps.Modules.Exceptions.Features.Policies.GetActivePolicy;

public sealed record GetActivePolicyQuery(string ExceptionType);

public sealed record ActivePolicyResponse(
    Guid Id,
    string PolicyKey,
    int VersionNumber,
    string ExceptionType,
    string Status,
    DateTimeOffset EffectiveFromUtc,
    string RuleDefinitionJson,
    string SeverityDefinitionJson,
    string AssignmentDefinitionJson,
    Guid? EvidencePolicyVersionId,
    Guid? SlaPolicyVersionId);

namespace ResolveOps.Modules.Exceptions.Features.Policies.CreatePolicyVersion;

public sealed record CreatePolicyVersionCommand(
    string PolicyKey,
    int VersionNumber,
    string ExceptionType,
    string RuleDefinitionJson,
    string SeverityDefinitionJson,
    string AssignmentDefinitionJson,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc = null,
    Guid? EvidencePolicyVersionId = null,
    Guid? SlaPolicyVersionId = null,
    bool ActivateNow = false);

public sealed record CreatePolicyVersionResponse(
    Guid Id,
    string PolicyKey,
    int VersionNumber,
    string ExceptionType,
    string Status,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc);

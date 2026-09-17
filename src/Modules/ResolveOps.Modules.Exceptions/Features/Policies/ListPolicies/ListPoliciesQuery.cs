namespace ResolveOps.Modules.Exceptions.Features.Policies.ListPolicies;

public sealed record ListPoliciesQuery(
    string? ExceptionType = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PolicySummaryResponse(
    Guid Id,
    string PolicyKey,
    int VersionNumber,
    string ExceptionType,
    string Status,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record ListPoliciesResponse(
    IReadOnlyList<PolicySummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

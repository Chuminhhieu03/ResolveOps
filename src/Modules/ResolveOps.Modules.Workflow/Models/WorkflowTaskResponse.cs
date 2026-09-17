namespace ResolveOps.Modules.Workflow.Models;

public sealed record WorkflowTaskResponse(
    Guid Id,
    Guid CaseId,
    Guid? ClaimId,
    string TaskType,
    string Title,
    string? Description,
    string Status,
    string Priority,
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    DateTimeOffset? DueAtUtc,
    string? BlockedReason,
    string? CompletionNote,
    DateTimeOffset? CompletedAtUtc,
    bool IsMandatory,
    string? WaivedReason,
    Guid? WaivedByUserId,
    DateTimeOffset? WaivedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string ConcurrencyStamp);

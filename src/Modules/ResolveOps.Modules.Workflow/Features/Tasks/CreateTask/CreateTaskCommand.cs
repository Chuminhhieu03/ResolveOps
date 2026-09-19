namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed record CreateTaskCommand(
    Guid CaseId,
    string TaskType,
    string Title,
    string? Description,
    string? Priority,
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    DateTimeOffset? DueAtUtc,
    bool IsMandatory);

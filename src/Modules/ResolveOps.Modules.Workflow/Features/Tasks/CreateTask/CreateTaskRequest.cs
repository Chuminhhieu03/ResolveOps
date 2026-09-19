namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed record CreateTaskRequest(
    string TaskType,
    string Title,
    string? Description = null,
    string? Priority = null,
    Guid? OwnerUserId = null,
    string? OwnerTeamCode = null,
    DateTimeOffset? DueAtUtc = null,
    bool IsMandatory = false);

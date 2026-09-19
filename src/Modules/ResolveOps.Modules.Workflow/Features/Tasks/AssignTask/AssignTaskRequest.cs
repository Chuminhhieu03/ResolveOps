namespace ResolveOps.Modules.Workflow.Features.Tasks.AssignTask;

public sealed record AssignTaskRequest(
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    string ConcurrencyStamp);

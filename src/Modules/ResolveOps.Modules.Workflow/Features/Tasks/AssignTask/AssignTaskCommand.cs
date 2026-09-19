namespace ResolveOps.Modules.Workflow.Features.Tasks.AssignTask;

public sealed record AssignTaskCommand(
    Guid TaskId,
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    string ConcurrencyStamp);

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetMyTasks;

public sealed record GetMyTasksQuery(Guid UserId, string? Status = null, string? Priority = null);

namespace ResolveOps.Modules.Workflow.Features.Tasks.CancelTask;

public sealed record CancelTaskCommand(Guid TaskId, string ConcurrencyStamp);

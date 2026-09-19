namespace ResolveOps.Modules.Workflow.Features.Tasks.StartTask;

public sealed record StartTaskCommand(Guid TaskId, string ConcurrencyStamp);

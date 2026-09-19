namespace ResolveOps.Modules.Workflow.Features.Tasks.WaiveTask;

public sealed record WaiveTaskCommand(Guid TaskId, string Reason, string ConcurrencyStamp);

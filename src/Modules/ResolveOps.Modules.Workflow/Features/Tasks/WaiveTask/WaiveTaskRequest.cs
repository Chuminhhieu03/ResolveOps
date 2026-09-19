namespace ResolveOps.Modules.Workflow.Features.Tasks.WaiveTask;

public sealed record WaiveTaskRequest(string Reason, string ConcurrencyStamp);

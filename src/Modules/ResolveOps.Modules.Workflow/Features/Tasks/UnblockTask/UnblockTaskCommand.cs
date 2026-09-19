namespace ResolveOps.Modules.Workflow.Features.Tasks.UnblockTask;

public sealed record UnblockTaskCommand(Guid TaskId, string ConcurrencyStamp);

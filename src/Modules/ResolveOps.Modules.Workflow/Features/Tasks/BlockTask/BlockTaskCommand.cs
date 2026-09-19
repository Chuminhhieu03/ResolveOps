namespace ResolveOps.Modules.Workflow.Features.Tasks.BlockTask;

public sealed record BlockTaskCommand(Guid TaskId, string Reason, string ConcurrencyStamp);

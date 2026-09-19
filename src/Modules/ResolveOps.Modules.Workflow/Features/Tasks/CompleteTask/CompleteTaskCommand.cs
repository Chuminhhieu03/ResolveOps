namespace ResolveOps.Modules.Workflow.Features.Tasks.CompleteTask;

public sealed record CompleteTaskCommand(Guid TaskId, string? CompletionNote, string ConcurrencyStamp);

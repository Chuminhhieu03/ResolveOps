namespace ResolveOps.Modules.Workflow.Features.Tasks.CompleteTask;

public sealed record CompleteTaskRequest(string? CompletionNote, string ConcurrencyStamp);

using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CompleteTask;

internal sealed class CompleteTaskHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CompleteTaskHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        CompleteTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.WorkflowTasks
            .FirstOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return DomainError.Failure("ERR_TASK_NOT_FOUND", $"Task '{command.TaskId}' was not found.");
        }

        if (!string.Equals(task.ConcurrencyStamp, command.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return DomainError.ConcurrencyConflict;
        }

        var completeResult = task.Complete(command.CompletionNote, _timeProvider);
        if (!completeResult.IsSuccess)
        {
            return completeResult;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            WorkflowMetrics.TasksCompletedTotal.Add(1);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict;
        }

        return Result.Success();
    }
}

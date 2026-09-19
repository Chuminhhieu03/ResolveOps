using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.UnblockTask;

internal sealed class UnblockTaskHandler
{
    private readonly AppDbContext _dbContext;

    public UnblockTaskHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(
        UnblockTaskCommand command,
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

        var unblockResult = task.Unblock();
        if (!unblockResult.IsSuccess)
        {
            return unblockResult;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict;
        }

        return Result.Success();
    }
}

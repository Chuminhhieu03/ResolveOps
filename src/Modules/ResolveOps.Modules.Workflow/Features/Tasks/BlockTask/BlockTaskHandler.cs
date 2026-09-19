using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.BlockTask;

internal sealed class BlockTaskHandler
{
    private readonly AppDbContext _dbContext;

    public BlockTaskHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(
        BlockTaskCommand command,
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

        var blockResult = task.Block(command.Reason);
        if (!blockResult.IsSuccess)
        {
            return blockResult;
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

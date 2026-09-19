using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.AssignTask;

internal sealed class AssignTaskHandler
{
    private readonly AppDbContext _dbContext;

    public AssignTaskHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(
        AssignTaskCommand command,
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

        var assignResult = task.Assign(command.OwnerUserId, command.OwnerTeamCode);
        if (!assignResult.IsSuccess)
        {
            return assignResult;
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

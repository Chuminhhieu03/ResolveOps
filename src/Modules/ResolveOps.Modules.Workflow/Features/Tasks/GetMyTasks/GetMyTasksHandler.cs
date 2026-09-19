using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetMyTasks;

internal sealed class GetMyTasksHandler
{
    private readonly AppDbContext _dbContext;

    public GetMyTasksHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<WorkflowTaskResponse>>> HandleAsync(
        GetMyTasksQuery query,
        CancellationToken cancellationToken)
    {
        var dbQuery = _dbContext.WorkflowTasks
            .AsNoTracking()
            .Where(t => t.OwnerUserId == query.UserId);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(t => t.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            dbQuery = dbQuery.Where(t => t.Priority == query.Priority);
        }

        var tasks = await dbQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var responses = tasks.Select(t => t.ToResponse()).ToList();
        return Result<IReadOnlyList<WorkflowTaskResponse>>.Success(responses);
    }
}

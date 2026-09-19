using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetCaseTasks;

internal sealed class GetCaseTasksHandler
{
    private readonly AppDbContext _dbContext;

    public GetCaseTasksHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<WorkflowTaskResponse>>> HandleAsync(
        GetCaseTasksQuery query,
        CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.WorkflowTasks
            .AsNoTracking()
            .Where(t => t.CaseId == query.CaseId)
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var responses = tasks.Select(t => t.ToResponse()).ToList();
        return Result<IReadOnlyList<WorkflowTaskResponse>>.Success(responses);
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetCaseTasks;

public sealed record GetCaseTasksQuery(Guid CaseId);

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

public sealed class GetCaseTasksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-cases/{caseId:guid}/tasks", async (
            Guid caseId,
            GetCaseTasksHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCaseTasksQuery(caseId);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetCaseTasks")
        .WithTags("Tasks")
        .Produces<IReadOnlyList<WorkflowTaskResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

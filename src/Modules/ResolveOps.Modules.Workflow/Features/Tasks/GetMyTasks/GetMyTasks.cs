using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetMyTasks;

public sealed record GetMyTasksQuery(Guid UserId, string? Status = null, string? Priority = null);

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

public sealed class GetMyTasksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tasks/my", async (
            string? status,
            string? priority,
            GetMyTasksHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Results.Unauthorized();
            }

            var query = new GetMyTasksQuery(userId, status, priority);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetMyTasks")
        .WithTags("Tasks")
        .Produces<IReadOnlyList<WorkflowTaskResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);
    }
}

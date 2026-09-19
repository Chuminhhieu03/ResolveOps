using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetMyTasks;

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

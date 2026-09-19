using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.GetCaseTasks;

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

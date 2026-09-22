using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.WaiveTask;

public sealed class WaiveTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:guid}/waive", async (
            Guid taskId,
            WaiveTaskRequest request,
            IValidator<WaiveTaskCommand> validator,
            WaiveTaskHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new WaiveTaskCommand(taskId, request.Reason, request.ConcurrencyStamp);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var currentUserId = httpContext.GetUserId();

            var result = await handler.HandleAsync(command, currentUserId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("WaiveTask")
        .WithTags("Tasks")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireCloseCase);
    }
}

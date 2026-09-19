using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.AssignTask;

public sealed class AssignTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:guid}/assign", async (
            Guid taskId,
            AssignTaskRequest request,
            IValidator<AssignTaskCommand> validator,
            AssignTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignTaskCommand(
                taskId,
                request.OwnerUserId,
                request.OwnerTeamCode,
                request.ConcurrencyStamp);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("AssignTask")
        .WithTags("Tasks")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireAssignCase);
    }
}

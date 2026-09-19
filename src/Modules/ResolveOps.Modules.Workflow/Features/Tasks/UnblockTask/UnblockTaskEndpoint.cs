using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.UnblockTask;

public sealed class UnblockTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:guid}/unblock", async (
            Guid taskId,
            UnblockTaskRequest request,
            IValidator<UnblockTaskCommand> validator,
            UnblockTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UnblockTaskCommand(taskId, request.ConcurrencyStamp);

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
        .WithName("UnblockTask")
        .WithTags("Tasks")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUpdateCase);
    }
}

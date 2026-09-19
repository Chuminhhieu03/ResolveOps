using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.BlockTask;

public sealed class BlockTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks/{taskId:guid}/block", async (
            Guid taskId,
            BlockTaskRequest request,
            IValidator<BlockTaskCommand> validator,
            BlockTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new BlockTaskCommand(taskId, request.Reason, request.ConcurrencyStamp);

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
        .WithName("BlockTask")
        .WithTags("Tasks")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireUpdateCase);
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed class CreateTaskEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{caseId:guid}/tasks", async (
            Guid caseId,
            CreateTaskRequest request,
            IValidator<CreateTaskCommand> validator,
            CreateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateTaskCommand(
                caseId,
                request.TaskType,
                request.Title,
                request.Description,
                request.Priority,
                request.OwnerUserId,
                request.OwnerTeamCode,
                request.DueAtUtc,
                request.IsMandatory);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/exception-cases/{caseId}/tasks", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateTask")
        .WithTags("Tasks")
        .Produces<WorkflowTaskResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireUpdateCase);
    }
}

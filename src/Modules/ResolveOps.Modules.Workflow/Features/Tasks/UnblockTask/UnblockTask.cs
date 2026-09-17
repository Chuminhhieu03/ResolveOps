using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.UnblockTask;

public sealed record UnblockTaskRequest(string ConcurrencyStamp);
public sealed record UnblockTaskCommand(Guid TaskId, string ConcurrencyStamp);

public sealed class UnblockTaskValidator : AbstractValidator<UnblockTaskCommand>
{
    public UnblockTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ConcurrencyStamp).NotEmpty();
    }
}

internal sealed class UnblockTaskHandler
{
    private readonly AppDbContext _dbContext;

    public UnblockTaskHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> HandleAsync(
        UnblockTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.WorkflowTasks
            .FirstOrDefaultAsync(t => t.Id == command.TaskId, cancellationToken);

        if (task is null)
        {
            return DomainError.Failure("ERR_TASK_NOT_FOUND", $"Task '{command.TaskId}' was not found.");
        }

        if (!string.Equals(task.ConcurrencyStamp, command.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return DomainError.ConcurrencyConflict;
        }

        var unblockResult = task.Unblock();
        if (!unblockResult.IsSuccess)
        {
            return unblockResult;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict;
        }

        return Result.Success();
    }
}

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

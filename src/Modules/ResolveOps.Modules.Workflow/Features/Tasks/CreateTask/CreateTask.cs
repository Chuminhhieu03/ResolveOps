using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Workflow;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

public sealed record CreateTaskRequest(
    string TaskType,
    string Title,
    string? Description = null,
    string? Priority = null,
    Guid? OwnerUserId = null,
    string? OwnerTeamCode = null,
    DateTimeOffset? DueAtUtc = null,
    bool IsMandatory = false);

public sealed record CreateTaskCommand(
    Guid CaseId,
    string TaskType,
    string Title,
    string? Description,
    string? Priority,
    Guid? OwnerUserId,
    string? OwnerTeamCode,
    DateTimeOffset? DueAtUtc,
    bool IsMandatory);

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(250);
        RuleFor(x => x.TaskType).NotEmpty().MaximumLength(50);
    }
}

internal sealed class CreateTaskHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public CreateTaskHandler(AppDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<WorkflowTaskResponse>> HandleAsync(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId.Value;

        var exceptionCase = await _dbContext.ExceptionCases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CaseId, cancellationToken);

        if (exceptionCase is null)
        {
            return DomainError.Failure("ERR_EXCEPTION_CASE_NOT_FOUND", $"Exception case '{command.CaseId}' not found.");
        }

        var task = WorkflowTask.Create(
            tenantId,
            command.CaseId,
            command.TaskType,
            command.Title,
            command.Description,
            command.Priority ?? WorkflowTaskPriority.Normal,
            command.OwnerUserId,
            command.OwnerTeamCode,
            command.DueAtUtc,
            command.IsMandatory,
            createdByPolicyId: null,
            _timeProvider);

        _dbContext.WorkflowTasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<WorkflowTaskResponse>.Success(task.ToResponse());
    }
}

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

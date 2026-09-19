using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Workflow;
using ResolveOps.Modules.Workflow.Models;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Workflow.Features.Tasks.CreateTask;

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

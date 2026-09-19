using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Workflow;
using ResolveOps.Observability;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CloseCase;

internal sealed class CloseCaseHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CloseCaseHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        CloseCaseCommand command,
        Guid? currentUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var exceptionCase = await _dbContext.ExceptionCases
            .FirstOrDefaultAsync(c => c.Id == command.CaseId, cancellationToken);

        if (exceptionCase is null)
        {
            return DomainError.ResourceNotFound;
        }

        if (!string.Equals(exceptionCase.ConcurrencyStamp, command.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return DomainError.ConcurrencyConflict;
        }

        // Spec §10.3 Invariant 6: A case cannot close with mandatory incomplete tasks unless each task is waived by an authorized actor with reason
        var hasIncompleteMandatoryTasks = await _dbContext.WorkflowTasks
            .AnyAsync(
                t => t.CaseId == command.CaseId &&
                     t.IsMandatory &&
                     (t.Status == WorkflowTaskStatus.Open ||
                      t.Status == WorkflowTaskStatus.InProgress ||
                      t.Status == WorkflowTaskStatus.Blocked) &&
                     t.WaivedAtUtc == null,
                cancellationToken);

        var closeResult = exceptionCase.Close(
            command.Notes,
            hasIncompleteMandatoryTasks,
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!closeResult.IsSuccess)
        {
            return closeResult;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            WorkflowMetrics.CaseTransitionsTotal.Add(1);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DomainError.ConcurrencyConflict;
        }

        return Result.Success();
    }
}

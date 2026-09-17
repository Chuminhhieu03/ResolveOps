using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CancelExceptionCase;

internal sealed class CancelExceptionCaseHandler
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CancelExceptionCaseHandler(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        CancelExceptionCaseCommand command,
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

        // Optimistic concurrency check (ADR-006)
        if (!string.Equals(exceptionCase.ConcurrencyStamp, command.ConcurrencyStamp, StringComparison.Ordinal))
        {
            return DomainError.ConcurrencyConflict;
        }

        var cancelResult = exceptionCase.Cancel(
            command.Reason,
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!cancelResult.IsSuccess)
        {
            return cancelResult;
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

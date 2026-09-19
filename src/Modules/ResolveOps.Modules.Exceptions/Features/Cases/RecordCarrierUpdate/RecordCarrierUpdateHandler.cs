using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.RecordCarrierUpdate;

internal sealed class RecordCarrierUpdateHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ISlaClockService _slaClockService;
    private readonly TimeProvider _timeProvider;

    public RecordCarrierUpdateHandler(
        AppDbContext dbContext,
        ISlaClockService slaClockService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _slaClockService = slaClockService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        RecordCarrierUpdateCommand command,
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

        var priorStatus = exceptionCase.Status;

        var recordResult = exceptionCase.RecordCarrierUpdate(
            command.Notes,
            command.TargetStatus,
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!recordResult.IsSuccess)
        {
            return recordResult;
        }

        if (exceptionCase.Status == ExceptionCaseStatus.AwaitingCarrier && priorStatus != ExceptionCaseStatus.AwaitingCarrier)
        {
            await _slaClockService.PauseClocksAsync(
                exceptionCase.TenantId,
                exceptionCase.Id,
                reasonCode: "CARRIER_UPDATE_REQUESTED",
                actorId: currentUserId,
                cancellationToken);
        }
        else if (priorStatus == ExceptionCaseStatus.AwaitingCarrier && exceptionCase.Status == ExceptionCaseStatus.Investigating)
        {
            await _slaClockService.ResumeClocksAsync(
                exceptionCase.TenantId,
                exceptionCase.Id,
                currentUserId,
                cancellationToken);
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

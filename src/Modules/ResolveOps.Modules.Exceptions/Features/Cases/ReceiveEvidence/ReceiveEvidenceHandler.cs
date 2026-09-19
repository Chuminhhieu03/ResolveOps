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

namespace ResolveOps.Modules.Exceptions.Features.Cases.ReceiveEvidence;

internal sealed class ReceiveEvidenceHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ISlaClockService _slaClockService;
    private readonly TimeProvider _timeProvider;

    public ReceiveEvidenceHandler(
        AppDbContext dbContext,
        ISlaClockService slaClockService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _slaClockService = slaClockService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        ReceiveEvidenceCommand command,
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

        var receiveResult = exceptionCase.ReceiveEvidence(
            command.Notes,
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!receiveResult.IsSuccess)
        {
            return receiveResult;
        }

        // Resume SLA clocks with deadline adjusted for paused time (spec §9.4, §15.8)
        await _slaClockService.ResumeClocksAsync(
            exceptionCase.TenantId,
            exceptionCase.Id,
            currentUserId,
            cancellationToken);

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

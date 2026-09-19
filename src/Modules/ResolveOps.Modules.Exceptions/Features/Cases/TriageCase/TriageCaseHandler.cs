using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Domain.Workflow;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.TriageCase;

internal sealed class TriageCaseHandler
{
    private readonly AppDbContext _dbContext;
    private readonly ISlaClockService _slaClockService;
    private readonly TimeProvider _timeProvider;

    public TriageCaseHandler(
        AppDbContext dbContext,
        ISlaClockService slaClockService,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _slaClockService = slaClockService;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        TriageCaseCommand command,
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

        var triageResult = exceptionCase.Triage(
            actorId: currentUserId,
            actorType: ActorType.User,
            detailsJson: command.DetailsJson,
            correlationId: correlationId,
            timeProvider: _timeProvider);

        if (!triageResult.IsSuccess)
        {
            return triageResult;
        }

        // Complete FirstAction SLA clock upon triage (spec §8.4, §9.4)
        await _slaClockService.CompleteClockAsync(
            exceptionCase.TenantId,
            exceptionCase.Id,
            SlaClockType.FirstAction,
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

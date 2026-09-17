using Microsoft.EntityFrameworkCore;
using ResolveOps.Domain;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Exceptions.Features.Cases.GetExceptionCase;

internal sealed class GetExceptionCaseHandler
{
    private readonly AppDbContext _dbContext;

    public GetExceptionCaseHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ExceptionCaseDetailResponse>> HandleAsync(
        GetExceptionCaseQuery query,
        CancellationToken cancellationToken)
    {
        var exceptionCase = await _dbContext.ExceptionCases
            .AsNoTracking()
            .Include(c => c.Occurrences)
            .Include(c => c.TimelineEntries)
            .FirstOrDefaultAsync(c => c.Id == query.CaseId, cancellationToken);

        if (exceptionCase is null)
        {
            return DomainError.ResourceNotFound;
        }

        var occurrences = exceptionCase.Occurrences
            .OrderBy(o => o.ObservedAtUtc)
            .Select(o => new OccurrenceDetailResponse(
                o.Id,
                o.TrackingEventId,
                o.OccurrenceType,
                o.ObservedAtUtc,
                o.Summary,
                o.CreatedAtUtc))
            .ToList();

        var timeline = exceptionCase.TimelineEntries
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new TimelineEntryDetailResponse(
                t.Id,
                t.EntryType,
                t.ActorType,
                t.ActorId,
                t.Summary,
                t.DetailsJson,
                t.CreatedAtUtc,
                t.CorrelationId))
            .ToList();

        return Result<ExceptionCaseDetailResponse>.Success(new ExceptionCaseDetailResponse(
            exceptionCase.Id,
            exceptionCase.CaseNumber,
            exceptionCase.ShipmentId,
            exceptionCase.ShipmentLegId,
            exceptionCase.ExceptionType,
            exceptionCase.Fingerprint,
            exceptionCase.Status,
            exceptionCase.Severity,
            exceptionCase.SeverityScore,
            exceptionCase.PolicyId,
            exceptionCase.PolicyVersionNumber,
            exceptionCase.OwnerUserId,
            exceptionCase.OwnerTeamCode,
            exceptionCase.FinancialExposure,
            exceptionCase.ExposureCurrency,
            exceptionCase.RootCauseCode,
            exceptionCase.DispositionCode,
            exceptionCase.DetectedAtUtc,
            exceptionCase.ResolvedAtUtc,
            exceptionCase.ClosedAtUtc,
            exceptionCase.CreatedAtUtc,
            exceptionCase.ConcurrencyStamp,
            occurrences,
            timeline));
    }
}

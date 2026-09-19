using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.GetTimeline;

internal sealed class GetTimelineHandler
{
    private readonly AppDbContext _dbContext;

    public GetTimelineHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<CaseTimelineEntryResponse>>> HandleAsync(
        GetTimelineQuery query,
        CancellationToken cancellationToken)
    {
        var entries = await _dbContext.CaseTimelineEntries
            .AsNoTracking()
            .Where(t => t.CaseId == query.CaseId)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => new CaseTimelineEntryResponse(
                t.Id,
                t.CaseId,
                t.EntryType,
                t.ActorType,
                t.ActorId,
                t.Summary,
                t.DetailsJson,
                t.CreatedAtUtc,
                t.CorrelationId))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<CaseTimelineEntryResponse>>.Success(entries);
    }
}

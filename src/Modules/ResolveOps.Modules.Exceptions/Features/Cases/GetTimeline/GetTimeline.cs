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

public sealed record CaseTimelineEntryResponse(
    Guid Id,
    Guid CaseId,
    string EntryType,
    string ActorType,
    Guid? ActorId,
    string Summary,
    string? DetailsJson,
    DateTimeOffset CreatedAtUtc,
    string? CorrelationId);

public sealed record GetTimelineQuery(Guid CaseId);

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

public sealed class GetTimelineEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-cases/{id:guid}/timeline", async (
            Guid id,
            GetTimelineHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTimelineQuery(id);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetCaseTimeline")
        .WithTags("ExceptionCases")
        .Produces<IReadOnlyList<CaseTimelineEntryResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

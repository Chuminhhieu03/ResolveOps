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

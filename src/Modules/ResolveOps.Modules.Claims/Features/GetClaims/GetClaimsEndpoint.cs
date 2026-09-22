using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;

namespace ResolveOps.Modules.Claims.Features.GetClaims;

public sealed class GetClaimsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/claims", async (
            [FromQuery] Guid? caseId,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromServices] GetClaimsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetClaimsQuery(
                caseId,
                status,
                page > 0 ? page : 1,
                pageSize > 0 ? pageSize : 20);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetClaims")
        .WithTags("Claims");
    }
}

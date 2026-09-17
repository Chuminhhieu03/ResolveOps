using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.ListExceptionCases;

public sealed class ListExceptionCasesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-cases", async (
            string? status,
            string? severity,
            string? exceptionType,
            Guid? shipmentId,
            DateTimeOffset? fromUtc,
            DateTimeOffset? toUtc,
            int? page,
            int? pageSize,
            ListExceptionCasesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListExceptionCasesQuery(
                status,
                severity,
                exceptionType,
                shipmentId,
                fromUtc,
                toUtc,
                page ?? 1,
                pageSize ?? 20);

            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListExceptionCases")
        .WithTags("ExceptionCases")
        .Produces<ListExceptionCasesResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

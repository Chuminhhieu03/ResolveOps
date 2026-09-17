using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.GetExceptionCase;

public sealed class GetExceptionCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-cases/{id:guid}", async (
            Guid id,
            GetExceptionCaseHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetExceptionCaseQuery(id);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetExceptionCase")
        .WithTags("ExceptionCases")
        .Produces<ExceptionCaseDetailResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

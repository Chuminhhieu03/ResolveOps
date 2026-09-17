using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Policies.ListPolicies;

public sealed class ListPoliciesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-policies", async (
            string? exceptionType,
            string? status,
            int? page,
            int? pageSize,
            ListPoliciesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ListPoliciesQuery(
                exceptionType,
                status,
                page ?? 1,
                pageSize ?? 20);

            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListExceptionPolicies")
        .WithTags("ExceptionPolicies")
        .Produces<ListPoliciesResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

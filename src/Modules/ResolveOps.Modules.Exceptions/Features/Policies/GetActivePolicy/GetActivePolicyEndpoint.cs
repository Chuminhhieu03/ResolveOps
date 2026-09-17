using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Policies.GetActivePolicy;

public sealed class GetActivePolicyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exception-policies/active", async (
            string exceptionType,
            GetActivePolicyHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetActivePolicyQuery(exceptionType);
            var result = await handler.HandleAsync(query, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Ok(response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("GetActiveExceptionPolicy")
        .WithTags("ExceptionPolicies")
        .Produces<ActivePolicyResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCases);
    }
}

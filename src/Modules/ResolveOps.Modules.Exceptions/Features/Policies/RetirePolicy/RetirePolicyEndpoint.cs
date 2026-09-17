using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Policies.RetirePolicy;

public sealed class RetirePolicyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-policies/{id:guid}/retire", async (
            Guid id,
            RetirePolicyHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RetirePolicyCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("RetireExceptionPolicy")
        .WithTags("ExceptionPolicies")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManagePolicies);
    }
}

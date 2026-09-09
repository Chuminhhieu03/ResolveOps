using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.GetCustomer;

public static class GetCustomerEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers/{customerId:guid}", async (
            Guid customerId,
            GetCustomerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(customerId, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: _ => Results.NotFound());
        })
        .WithName("GetCustomer")
        .WithTags("Customers")
        .Produces<CustomerResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCustomers);
    }
}

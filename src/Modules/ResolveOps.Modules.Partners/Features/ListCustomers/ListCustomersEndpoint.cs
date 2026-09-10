using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListCustomers;

public sealed class ListCustomersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers", async (
            ListCustomersHandler handler,
            CancellationToken cancellationToken,
            string? priority = null,
            string? status = null,
            int page = 1,
            int pageSize = 50) =>
        {
            var result = await handler.HandleAsync(priority, status, page, pageSize, cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("ListCustomers")
        .WithTags("Customers")
        .Produces<ListCustomersResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCustomers);
    }
}

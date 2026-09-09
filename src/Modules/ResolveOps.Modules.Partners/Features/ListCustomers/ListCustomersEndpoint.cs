using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.ListCustomers;

public static class ListCustomersEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
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
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "List Customers Failed",
                    detail: error.Message));
        })
        .WithName("ListCustomers")
        .WithTags("Customers")
        .Produces<ListCustomersResponse>(StatusCodes.Status200OK)
        .RequireAuthorization(AuthorizationPolicies.RequireViewCustomers);
    }
}

using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetClaimsDashboard;

public sealed class GetClaimsDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/dashboard/claims", "/dashboard/claims" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                IValidator<GetClaimsDashboardQuery> validator,
                GetClaimsDashboardHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var query = new GetClaimsDashboardQuery(tenantId);

                var validation = await validator.ValidateAsync(query, cancellationToken);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var result = await handler.HandleAsync(query, cancellationToken);
                return result.Match(
                    success => Results.Ok(success),
                    error => Results.BadRequest(new { error.Code, error.Message }));
            })
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
            .WithName($"GetClaimsDashboard_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Dashboards");
        }
    }
}

using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Dashboards.GetOperationsDashboard;

public sealed class GetOperationsDashboardEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/dashboard/operations", "/dashboard/operations" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                IValidator<GetOperationsDashboardQuery> validator,
                GetOperationsDashboardHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var query = new GetOperationsDashboardQuery(tenantId);

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
            .WithName($"GetOperationsDashboard_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Dashboards");
        }
    }
}

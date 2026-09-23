using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetSlaPerformanceReport;

public sealed class GetSlaPerformanceReportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/reports/sla-performance", "/reports/sla-performance" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                Guid? carrierId,
                string? priority,
                DateTimeOffset? fromDate,
                DateTimeOffset? toDate,
                IValidator<GetSlaPerformanceReportQuery> validator,
                GetSlaPerformanceReportHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var query = new GetSlaPerformanceReportQuery(
                    TenantId: tenantId,
                    CarrierId: carrierId,
                    Priority: priority,
                    FromDate: fromDate,
                    ToDate: toDate);

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
            .WithName($"GetSlaPerformanceReport_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Reports");
        }
    }
}

using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetCarrierScorecardsReport;

public sealed class GetCarrierScorecardsReportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/reports/carrier-scorecards", "/reports/carrier-scorecards" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                Guid? carrierId,
                DateOnly? fromDate,
                DateOnly? toDate,
                IValidator<GetCarrierScorecardsReportQuery> validator,
                GetCarrierScorecardsReportHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var query = new GetCarrierScorecardsReportQuery(
                    TenantId: tenantId,
                    CarrierId: carrierId,
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
            .WithName($"GetCarrierScorecardsReport_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Reports");
        }
    }
}

using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Reports.GetExceptionAgeingReport;

public sealed class GetExceptionAgeingReportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/reports/exception-ageing", "/reports/exception-ageing" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                Guid? carrierId,
                string? severity,
                DateTimeOffset? fromDate,
                DateTimeOffset? toDate,
                IValidator<GetExceptionAgeingReportQuery> validator,
                GetExceptionAgeingReportHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var query = new GetExceptionAgeingReportQuery(
                    TenantId: tenantId,
                    CarrierId: carrierId,
                    Severity: severity,
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
            .WithName($"GetExceptionAgeingReport_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Reports");
        }
    }
}

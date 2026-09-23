using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Exports.GetExportStatus;

public sealed class GetExportStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/exports/{exportId:guid}", "/exports/{exportId:guid}" };

        foreach (var route in routes)
        {
            app.MapGet(route, async (
                Guid exportId,
                IValidator<GetExportStatusQuery> validator,
                GetExportStatusHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var userId = httpContext.GetUserId();

                var query = new GetExportStatusQuery(
                    TenantId: tenantId,
                    UserId: userId,
                    ExportId: exportId);

                var validation = await validator.ValidateAsync(query, cancellationToken);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var result = await handler.HandleAsync(query, cancellationToken);
                return result.Match(
                    success => Results.Ok(success),
                    error => error.Code == "FORBIDDEN"
                        ? Results.Forbid()
                        : error.Code == "ERR_EXPORT_NOT_FOUND"
                            ? Results.NotFound(new { error.Code, error.Message })
                            : Results.BadRequest(new { error.Code, error.Message }));
            })
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
            .WithName($"GetExportStatus_{route.Replace("/", "_").Replace("{", "").Replace("}", "").Replace(":", "_").Trim('_')}")
            .WithTags("Exports");
        }
    }
}

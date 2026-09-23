using System;
using System.Threading;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestExceptionCasesExport;

public sealed class RequestExceptionCasesExportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var routes = new[] { "/api/exports/exception-cases", "/exports/exception-cases" };

        foreach (var route in routes)
        {
            app.MapPost(route, async (
                [FromBody] ExportFiltersRequest? request,
                IValidator<RequestExceptionCasesExportCommand> validator,
                RequestExceptionCasesExportHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var tenantId = httpContext.GetTenantId();
                var userId = httpContext.GetUserId();

                var command = new RequestExceptionCasesExportCommand(
                    TenantId: tenantId,
                    UserId: userId,
                    CarrierId: request?.CarrierId,
                    Severity: request?.Severity,
                    FromDate: request?.FromDate,
                    ToDate: request?.ToDate);

                var validation = await validator.ValidateAsync(command, cancellationToken);
                if (!validation.IsValid)
                {
                    return Results.ValidationProblem(validation.ToDictionary());
                }

                var result = await handler.HandleAsync(command, cancellationToken);
                return result.Match(
                    success => Results.Accepted(success.PollUri, success),
                    error => Results.BadRequest(new { error.Code, error.Message }));
            })
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
            .WithName($"RequestExceptionCasesExport_{route.Replace("/", "_").Trim('_')}")
            .WithTags("Exports");
        }
    }

    public sealed record ExportFiltersRequest(
        Guid? CarrierId = null,
        string? Severity = null,
        DateTimeOffset? FromDate = null,
        DateTimeOffset? ToDate = null);
}

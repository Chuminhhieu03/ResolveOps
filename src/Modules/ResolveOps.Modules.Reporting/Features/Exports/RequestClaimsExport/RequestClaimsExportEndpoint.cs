using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestClaimsExport;

public sealed class RequestClaimsExportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exports/claims", HandleEndpointAsync)
            .RequireAuthorization()
            .WithName("RequestClaimsExport")
            .WithTags("Exports")
            .Produces<RequestClaimsExportResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/exports/claims", HandleEndpointAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleEndpointAsync(
        ClaimsExportRequestDto? requestDto,
        HttpContext httpContext,
        RequestClaimsExportHandler handler,
        IValidator<RequestClaimsExportCommand> validator,
        CancellationToken ct)
    {
        var tenantId = httpContext.GetTenantId();
        var userId = httpContext.GetUserId();

        var command = new RequestClaimsExportCommand(
            TenantId: tenantId,
            UserId: userId,
            CarrierId: requestDto?.CarrierId,
            Status: requestDto?.Status,
            FromDate: requestDto?.FromDate,
            ToDate: requestDto?.ToDate);

        var validation = await validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await handler.HandleAsync(command, ct);
        if (result.IsFailure)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Failed to queue claims export",
                detail: result.Error.Message);
        }

        return Results.Accepted(result.Value.PollUri, result.Value);
    }
}

public sealed record ClaimsExportRequestDto(
    Guid? CarrierId = null,
    string? Status = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);

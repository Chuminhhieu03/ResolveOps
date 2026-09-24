using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Exports.RequestCarrierScorecardsExport;

public sealed class RequestCarrierScorecardsExportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exports/carrier-scorecards", HandleEndpointAsync)
            .RequireAuthorization()
            .WithName("RequestCarrierScorecardsExport")
            .WithTags("Exports")
            .Produces<RequestCarrierScorecardsExportResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/exports/carrier-scorecards", HandleEndpointAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleEndpointAsync(
        CarrierScorecardsExportRequestDto? requestDto,
        HttpContext httpContext,
        RequestCarrierScorecardsExportHandler handler,
        IValidator<RequestCarrierScorecardsExportCommand> validator,
        CancellationToken ct)
    {
        var tenantId = httpContext.GetTenantId();
        var userId = httpContext.GetUserId();

        var command = new RequestCarrierScorecardsExportCommand(
            TenantId: tenantId,
            UserId: userId,
            CarrierId: requestDto?.CarrierId,
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
                title: "Failed to queue carrier scorecards export",
                detail: result.Error.Message);
        }

        return Results.Accepted(result.Value.PollUri, result.Value);
    }
}

public sealed record CarrierScorecardsExportRequestDto(
    Guid? CarrierId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);

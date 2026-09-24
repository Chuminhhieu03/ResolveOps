using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Reporting.Features.Exports.ListUserExports;

public sealed class ListUserExportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/exports", HandleEndpointAsync)
            .RequireAuthorization()
            .WithName("ListUserExports")
            .WithTags("Exports")
            .Produces<ListUserExportsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapGet("/exports", HandleEndpointAsync)
            .RequireAuthorization()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> HandleEndpointAsync(
        int? pageNumber,
        int? pageSize,
        HttpContext httpContext,
        ListUserExportsHandler handler,
        IValidator<ListUserExportsQuery> validator,
        CancellationToken ct)
    {
        var tenantId = httpContext.GetTenantId();
        var userId = httpContext.GetUserId();

        var query = new ListUserExportsQuery(
            TenantId: tenantId,
            UserId: userId,
            PageNumber: pageNumber ?? 1,
            PageSize: pageSize ?? 20);

        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(validation.ToDictionary());
        }

        var result = await handler.HandleAsync(query, ct);
        if (result.IsFailure)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Failed to list exports",
                detail: result.Error.Message);
        }

        return Results.Ok(result.Value);
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

public static class TenantSettingsEndpoint
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants/current/settings")
            .WithTags("Tenancy")
            .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership);

        group.MapGet("/", async (
            TenantSettingsHandlers handlers,
            CancellationToken cancellationToken) =>
        {
            var result = await handlers.HandleGetAsync(new GetTenantSettingsQuery(), cancellationToken);

            return result.Match(
                onSuccess: data => Results.Ok(data),
                onFailure: error => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Failed to get settings", detail: error.Message)
            );
        })
        .WithName("GetTenantSettings")
        .Produces<GetTenantSettingsResponse>(StatusCodes.Status200OK);

        group.MapPut("/", async (
            UpdateTenantSettingsCommand command,
            TenantSettingsHandlers handlers,
            IValidator<UpdateTenantSettingsCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handlers.HandleUpdateAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Code switch
                {
                    "CONCURRENCY_CONFLICT" => Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Concurrency Conflict", detail: error.Message),
                    _ => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Update Failed", detail: error.Message)
                }
            );
        })
        .WithName("UpdateTenantSettings")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

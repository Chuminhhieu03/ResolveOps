using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.TenantSettings;

public sealed class TenantSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
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
                onFailure: error => error.ToProblemDetails()
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
                onFailure: error => error.ToProblemDetails()
            );
        })
        .WithName("UpdateTenantSettings")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

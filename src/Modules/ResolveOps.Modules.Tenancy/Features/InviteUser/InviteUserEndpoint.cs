using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Tenancy.Features.InviteUser;

public static class InviteUserEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/tenancy/users/invite", async (
            [FromBody] InviteUserCommand command,
            [FromServices] InviteUserHandler handler,
            [FromServices] IValidator<InviteUserCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);
            if (!result)
            {
                return Results.BadRequest("Failed to invite user.");
            }

            return Results.Ok();
        })
        .RequireAuthorization(AuthorizationPolicies.RequireActiveTenantMembership)
        .RequireAuthorization(policy => policy.RequireClaim(Permissions.CanManageUsers))
        .WithName("InviteUser")
        .WithTags("Tenancy");
    }
}

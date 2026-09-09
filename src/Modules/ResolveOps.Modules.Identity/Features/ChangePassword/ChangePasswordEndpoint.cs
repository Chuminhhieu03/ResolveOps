using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ResolveOps.Modules.Identity.Features.ChangePassword;

public static class ChangePasswordEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/identity/me/password", async (
            ChangePasswordCommand command,
            ChangePasswordHandler handler,
            IValidator<ChangePasswordCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Failed to change password",
                    detail: error.Message)
            );
        })
        .WithName("ChangePassword")
        .WithTags("Identity")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireAuthorization();
    }
}

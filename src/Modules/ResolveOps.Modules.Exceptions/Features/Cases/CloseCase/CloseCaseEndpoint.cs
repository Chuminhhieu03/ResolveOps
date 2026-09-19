using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CloseCase;

public sealed class CloseCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{id:guid}/close", async (
            Guid id,
            CloseCaseRequest request,
            IValidator<CloseCaseCommand> validator,
            CloseCaseHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CloseCaseCommand(id, request.Notes, request.ConcurrencyStamp, request.DetailsJson);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? currentUserId = Guid.TryParse(userIdString, out var parsedId) ? parsedId : null;
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, currentUserId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CloseCase")
        .WithTags("ExceptionCases")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireCloseCase);
    }
}

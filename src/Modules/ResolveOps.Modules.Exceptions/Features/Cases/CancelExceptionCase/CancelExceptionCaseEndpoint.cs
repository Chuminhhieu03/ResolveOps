using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CancelExceptionCase;

public sealed class CancelExceptionCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{id:guid}/cancel", async (
            Guid id,
            CancelExceptionCaseRequest request,
            IValidator<CancelExceptionCaseCommand> validator,
            CancelExceptionCaseHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CancelExceptionCaseCommand(
                id,
                request.Reason,
                request.ConcurrencyStamp,
                request.DetailsJson);

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
        .WithName("CancelExceptionCase")
        .WithTags("ExceptionCases")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireCancelCase);
    }
}

public sealed record CancelExceptionCaseRequest(
    string Reason,
    string ConcurrencyStamp,
    string? DetailsJson = null);

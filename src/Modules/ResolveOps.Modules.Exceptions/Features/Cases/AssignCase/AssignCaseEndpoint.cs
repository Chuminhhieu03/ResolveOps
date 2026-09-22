using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application;
using ResolveOps.Domain;
using ResolveOps.Domain.Exceptions;
using ResolveOps.Observability;
using ResolveOps.Persistence;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.AssignCase;

public sealed class AssignCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases/{id:guid}/assign", async (
            Guid id,
            AssignCaseRequest request,
            IValidator<AssignCaseCommand> validator,
            AssignCaseHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignCaseCommand(
                id,
                request.OwnerUserId,
                request.OwnerTeamCode,
                request.Reason,
                request.ConcurrencyStamp,
                request.DetailsJson);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var currentUserId = httpContext.TryGetUserId();
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, currentUserId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("AssignExceptionCase")
        .WithTags("ExceptionCases")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization(AuthorizationPolicies.RequireAssignCase);
    }
}

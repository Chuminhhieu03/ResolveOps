using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Exceptions.Features.Cases.CreateManualCase;

public sealed class CreateManualCaseEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/exception-cases", async (
            CreateManualCaseRequest request,
            IValidator<CreateManualCaseCommand> validator,
            CreateManualCaseHandler handler,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateManualCaseCommand(
                request.ShipmentId,
                request.ShipmentLegId,
                request.ExceptionType,
                request.Reason,
                request.Severity,
                request.FinancialExposure,
                request.ExposureCurrency,
                request.OwnerTeamCode);

            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var currentUserId = httpContext.TryGetUserId();
            var correlationId = httpContext.TraceIdentifier;

            var result = await handler.HandleAsync(command, currentUserId, correlationId, cancellationToken);

            return result.Match(
                onSuccess: response => Results.Created($"/api/exception-cases/{response.Id}", response),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("CreateManualExceptionCase")
        .WithTags("ExceptionCases")
        .Produces<CreateManualCaseResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireCreateExceptionCase);
    }
}

public sealed record CreateManualCaseRequest(
    Guid ShipmentId,
    Guid? ShipmentLegId,
    string ExceptionType,
    string Reason,
    string? Severity = null,
    decimal? FinancialExposure = null,
    string? ExposureCurrency = null,
    string? OwnerTeamCode = null);

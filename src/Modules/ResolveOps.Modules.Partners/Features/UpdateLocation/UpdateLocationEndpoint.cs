using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Application;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateLocation;

public sealed class UpdateLocationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/locations/{locationId:guid}", async (
            Guid locationId,
            UpdateLocationRequest request,
            UpdateLocationHandler handler,
            IValidator<UpdateLocationCommand> validator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateLocationCommand(
                locationId,
                request.Name,
                request.AddressLine1,
                request.AddressLine2,
                request.City,
                request.Region,
                request.PostalCode,
                request.CountryCode,
                request.Timezone,
                request.Latitude,
                request.Longitude,
                request.ConcurrencyStamp);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.ToProblemDetails());
        })
        .WithName("UpdateLocation")
        .WithTags("Locations")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization(AuthorizationPolicies.RequireManageLocations);
    }
}

public sealed record UpdateLocationRequest(
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    string Timezone,
    decimal? Latitude,
    decimal? Longitude,
    string ConcurrencyStamp);

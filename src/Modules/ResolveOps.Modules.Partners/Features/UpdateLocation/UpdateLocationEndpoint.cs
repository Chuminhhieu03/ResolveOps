using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ResolveOps.Security;

namespace ResolveOps.Modules.Partners.Features.UpdateLocation;

public static class UpdateLocationEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
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
                request.ExpectedVersion);

            var validationResult = await validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var result = await handler.HandleAsync(command, cancellationToken);

            return result.Match(
                onSuccess: () => Results.NoContent(),
                onFailure: error => error.Code switch
                {
                    "CONCURRENCY_CONFLICT" => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Concurrency Conflict",
                        detail: error.Message),
                    "RESOURCE_NOT_FOUND" => Results.NotFound(),
                    _ => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Update Location Failed",
                        detail: error.Message),
                });
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
    long ExpectedVersion);

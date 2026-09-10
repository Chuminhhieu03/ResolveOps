namespace ResolveOps.Modules.Partners.Features.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid LocationId,
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

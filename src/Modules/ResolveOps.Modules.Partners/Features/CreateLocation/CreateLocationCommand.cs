namespace ResolveOps.Modules.Partners.Features.CreateLocation;

public sealed record CreateLocationCommand(
    string Code,
    string Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    string Timezone,
    decimal? Latitude,
    decimal? Longitude);

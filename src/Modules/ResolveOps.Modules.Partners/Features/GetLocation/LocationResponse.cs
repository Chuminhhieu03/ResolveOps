namespace ResolveOps.Modules.Partners.Features.GetLocation;

public sealed record LocationResponse(
    Guid Id,
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
    decimal? Longitude,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string ConcurrencyStamp);

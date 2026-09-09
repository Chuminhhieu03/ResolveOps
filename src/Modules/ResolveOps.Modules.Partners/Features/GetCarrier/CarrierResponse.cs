namespace ResolveOps.Modules.Partners.Features.GetCarrier;

public sealed record CarrierResponse(
    Guid Id,
    string Code,
    string Name,
    string Status,
    string? ScacOrExternalCode,
    string? DefaultTimezone,
    string? ContactEmail,
    string ClaimSubmissionChannel,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long Version);

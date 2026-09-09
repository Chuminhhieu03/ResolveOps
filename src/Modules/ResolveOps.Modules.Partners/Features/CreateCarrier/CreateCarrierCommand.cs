namespace ResolveOps.Modules.Partners.Features.CreateCarrier;

public sealed record CreateCarrierCommand(
    string Code,
    string Name,
    string? ScacOrExternalCode,
    string? DefaultTimezone,
    string? ContactEmail,
    string ClaimSubmissionChannel);

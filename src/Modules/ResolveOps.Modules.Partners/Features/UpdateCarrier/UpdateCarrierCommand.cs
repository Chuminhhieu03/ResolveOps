namespace ResolveOps.Modules.Partners.Features.UpdateCarrier;

public sealed record UpdateCarrierCommand(
    Guid CarrierId,
    string Name,
    string? ScacOrExternalCode,
    string? DefaultTimezone,
    string? ContactEmail,
    string ClaimSubmissionChannel,
    string ConcurrencyStamp);

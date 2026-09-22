namespace ResolveOps.Modules.Claims.Features.WriteOffClaim;

public record WriteOffClaimRequest(
    decimal WriteOffAmount,
    string Reason);

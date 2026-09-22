using System;

namespace ResolveOps.Modules.Claims.Features.SupplyAdditionalInformation;

public record SupplyAdditionalInformationResponse(Guid ClaimId, string Status);

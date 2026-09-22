using System;

namespace ResolveOps.Modules.Claims.Features.SupplyAdditionalInformation;

public record SupplyAdditionalInformationCommand(Guid ClaimId, string ResponseNotes, Guid RecordedBy);

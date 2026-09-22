using System;

namespace ResolveOps.Modules.Claims.Features.ReturnToDraft;

public record ReturnToDraftResponse(Guid ClaimId, string Status, string Reason);

using System;

namespace ResolveOps.Modules.Claims.Features.ReturnToDraft;

public record ReturnToDraftCommand(Guid ClaimId, Guid ReviewerId, string Reason);

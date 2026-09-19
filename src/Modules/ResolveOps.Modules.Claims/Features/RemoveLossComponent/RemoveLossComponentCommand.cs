using System;

namespace ResolveOps.Modules.Claims.Features.RemoveLossComponent;

public record RemoveLossComponentCommand(Guid ClaimId, Guid ComponentId);

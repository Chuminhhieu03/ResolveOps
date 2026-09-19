using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Claims.Features.GetClaimReadiness;

public record GetClaimReadinessResponse(
    bool IsReady,
    IReadOnlyList<EvidenceCheckItemResponse> Checklist,
    string[] MissingRequirements);

public record EvidenceCheckItemResponse(
    Guid? DocumentId,
    string EvidenceType,
    bool IsMandatory,
    bool IsSatisfied);

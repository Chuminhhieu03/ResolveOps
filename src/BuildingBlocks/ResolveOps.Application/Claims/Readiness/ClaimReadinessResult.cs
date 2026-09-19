using System;
using System.Collections.Generic;

namespace ResolveOps.Application.Claims.Readiness;

public record EvidenceRequirementCheckItem(
    Guid? DocumentId,
    string EvidenceType,
    bool IsMandatory,
    bool IsSatisfied);

public record ClaimReadinessResult(
    bool IsReady,
    IReadOnlyList<EvidenceRequirementCheckItem> Checklist,
    string[] MissingRequirements);

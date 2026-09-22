using System;

namespace ResolveOps.Modules.Claims.Features.ApproveForSubmission;

public record ApproveForSubmissionResponse(
    Guid ClaimId,
    string Status,
    Guid ApprovedBy,
    DateTimeOffset ApprovedAtUtc);

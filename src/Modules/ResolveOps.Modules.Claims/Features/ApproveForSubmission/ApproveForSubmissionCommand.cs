using System;

namespace ResolveOps.Modules.Claims.Features.ApproveForSubmission;

public record ApproveForSubmissionCommand(Guid ClaimId, Guid ApproverId, string? Note);

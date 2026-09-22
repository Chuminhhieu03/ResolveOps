using System;

namespace ResolveOps.Modules.Claims.Features.RequestReview;

public record RequestReviewCommand(Guid ClaimId, Guid RequestedBy);

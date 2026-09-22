using System;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public record RecordSubmissionResponse(
    Guid ClaimId,
    string Status,
    string ExternalReference,
    DateTimeOffset SubmittedAtUtc);

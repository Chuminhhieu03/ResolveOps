using System;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public record RecordSubmissionCommand(Guid ClaimId, string ExternalReference, DateTimeOffset SubmittedAtUtc);

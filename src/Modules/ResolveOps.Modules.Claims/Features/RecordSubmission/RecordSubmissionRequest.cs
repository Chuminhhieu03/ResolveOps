using System;

namespace ResolveOps.Modules.Claims.Features.RecordSubmission;

public record RecordSubmissionRequest(string ExternalReference, DateTimeOffset SubmittedAtUtc);

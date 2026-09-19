using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Claims.Features.GetClaimById;

public record GetClaimByIdResponse(
    Guid Id,
    string ClaimNumber,
    Guid CaseId,
    Guid CarrierId,
    string ClaimType,
    string Status,
    string EligibilityStatus,
    string[] EligibilityReasonCodes,
    DateTimeOffset? ClaimDeadlineAtUtc,
    decimal ClaimedAmount,
    decimal ApprovedAmount,
    decimal RecoveredAmount,
    string Currency,
    string? ExternalSubmissionReference,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<LossComponentResponse> LossComponents);

public record LossComponentResponse(
    Guid Id,
    string ComponentType,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency,
    Guid? SourceDocumentId);

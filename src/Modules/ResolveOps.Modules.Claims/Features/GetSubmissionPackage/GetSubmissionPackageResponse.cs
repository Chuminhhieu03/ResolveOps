using System;
using System.Collections.Generic;

namespace ResolveOps.Modules.Claims.Features.GetSubmissionPackage;

public record ClaimSubmissionPackageHeader(
    Guid ClaimId,
    string ClaimNumber,
    string ClaimType,
    string Status,
    string Currency,
    decimal ClaimedAmount,
    DateTimeOffset? DeadlineAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    string? ExternalSubmissionReference);

public record ClaimSubmissionPackageCarrier(
    Guid CarrierId,
    string Name,
    string Code);

public record ClaimSubmissionPackageCase(
    Guid CaseId,
    string CaseNumber,
    string ExceptionType,
    string Severity);

public record ClaimSubmissionPackageLossComponent(
    Guid Id,
    string ComponentType,
    string Description,
    decimal? Quantity,
    decimal? UnitAmount,
    decimal Amount,
    string Currency);

public record ClaimSubmissionPackageEvidenceDocument(
    Guid DocumentId,
    string EvidenceType,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DownloadUrl,
    DateTimeOffset ExpiresAtUtc);

public record GetSubmissionPackageResponse(
    ClaimSubmissionPackageHeader Header,
    ClaimSubmissionPackageCarrier Carrier,
    ClaimSubmissionPackageCase Case,
    IReadOnlyList<ClaimSubmissionPackageLossComponent> LossComponents,
    IReadOnlyList<ClaimSubmissionPackageEvidenceDocument> EvidenceDocuments);

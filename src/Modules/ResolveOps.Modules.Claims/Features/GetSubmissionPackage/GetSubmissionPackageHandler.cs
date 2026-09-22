using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResolveOps.Application.Documents;
using ResolveOps.Domain;
using ResolveOps.Domain.Documents;
using ResolveOps.Persistence;

namespace ResolveOps.Modules.Claims.Features.GetSubmissionPackage;

public class GetSubmissionPackageHandler
{
    private static readonly TimeSpan _downloadUrlExpiry = TimeSpan.FromHours(1);

    private readonly AppDbContext _dbContext;
    private readonly IObjectStorageService _objectStorage;
    private readonly TimeProvider _timeProvider;

    public GetSubmissionPackageHandler(
        AppDbContext dbContext,
        IObjectStorageService objectStorage,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _objectStorage = objectStorage;
        _timeProvider = timeProvider;
    }

    public async Task<Result<GetSubmissionPackageResponse>> HandleAsync(
        GetSubmissionPackageQuery query,
        CancellationToken cancellationToken)
    {
        var claim = await _dbContext.Claims
            .Include(c => c.LossComponents)
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken);

        if (claim == null)
        {
            return Result<GetSubmissionPackageResponse>.Failure(new DomainError("CLAIM_NOT_FOUND", "The specified claim was not found."));
        }

        var carrier = await _dbContext.Carriers
            .FirstOrDefaultAsync(c => c.Id == claim.CarrierId, cancellationToken);

        var excCase = await _dbContext.ExceptionCases
            .FirstOrDefaultAsync(c => c.Id == claim.CaseId, cancellationToken);

        var header = new ClaimSubmissionPackageHeader(
            claim.Id,
            claim.ClaimNumber,
            claim.ClaimType,
            claim.Status,
            claim.Currency,
            claim.ClaimedAmount,
            claim.ClaimDeadlineAtUtc,
            claim.SubmittedAtUtc,
            claim.ExternalSubmissionReference);

        var carrierInfo = new ClaimSubmissionPackageCarrier(
            carrier?.Id ?? claim.CarrierId,
            carrier?.Name ?? string.Empty,
            carrier?.Code ?? string.Empty);

        var caseInfo = new ClaimSubmissionPackageCase(
            excCase?.Id ?? claim.CaseId,
            excCase?.CaseNumber ?? string.Empty,
            excCase?.ExceptionType ?? string.Empty,
            excCase?.Severity ?? string.Empty);

        var lossComponents = claim.LossComponents.Select(lc => new ClaimSubmissionPackageLossComponent(
            lc.Id,
            lc.ComponentType,
            lc.Description,
            lc.Quantity,
            lc.UnitAmount,
            lc.Amount.Amount,
            lc.Amount.Currency)).ToList();

        // Query clean, available evidence documents for the case and claim
        var evidenceDocs = await _dbContext.EvidenceDocuments
            .Where(d => (d.CaseId == claim.CaseId || d.ClaimId == claim.Id) &&
                        d.Status == DocumentStatus.Available &&
                        d.ScanStatus == DocumentScanStatus.Clean)
            .ToListAsync(cancellationToken);

        var docList = new List<ClaimSubmissionPackageEvidenceDocument>();
        var now = _timeProvider.GetUtcNow();

        foreach (var doc in evidenceDocs)
        {
            var downloadUrl = await _objectStorage.GenerateDownloadPresignedUrlAsync(
                doc.StorageContainer,
                doc.StorageObjectName,
                _downloadUrlExpiry,
                doc.OriginalFileName,
                cancellationToken);

            docList.Add(new ClaimSubmissionPackageEvidenceDocument(
                doc.Id,
                doc.EvidenceType,
                doc.OriginalFileName,
                doc.ContentType,
                doc.SizeBytes,
                downloadUrl,
                now.Add(_downloadUrlExpiry)));
        }

        return Result<GetSubmissionPackageResponse>.Success(new GetSubmissionPackageResponse(
            header,
            carrierInfo,
            caseInfo,
            lossComponents,
            docList));
    }
}

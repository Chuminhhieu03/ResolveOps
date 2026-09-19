namespace ResolveOps.Domain.Documents;

/// <summary>
/// EvidenceDocument aggregate root (spec §8.6, §10.4, §15.9).
///
/// Invariants enforced here (spec §10.4):
/// 1. Document is unavailable to business workflows until ScanStatus is Clean and Status is Available.
/// 2. Document belongs to exactly one tenant.
/// 3. Storage object path uses a non-guessable tenant-partitioned path (set by application layer).
/// 4. File extension is never trusted; contentType is validated against the allowlist before creation.
/// 5. SHA-256 checksum is stored to identify duplicates (validated by DocumentProcessingWorker).
/// 6. Superseding does not erase prior versions; older document transitions to Superseded.
/// 7. Document under LegalHold cannot be removed.
/// 8. ConcurrencyStamp is managed centrally by AppDbContext.ApplyAuditAndConcurrency() (ADR-006).
/// </summary>
public sealed class EvidenceDocument : IHasConcurrencyStamp
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid? ClaimId { get; private set; }

    public string EvidenceType { get; private set; } = string.Empty;
    public string Status { get; private set; } = DocumentStatus.PendingUpload;

    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>
    /// Non-guessable storage object key. Format set by application layer:
    /// <c>tenants/{tenantId}/cases/{caseId}/{evidenceType}/{documentId}_{safeFileName}</c>
    /// </summary>
    public string StorageObjectName { get; private set; } = string.Empty;

    /// <summary>MinIO bucket name (container).</summary>
    public string StorageContainer { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }

    /// <summary>SHA-256 hex string (lowercase, 64 chars). Set during CompleteUpload; verified by worker.</summary>
    public string? Sha256 { get; private set; }

    public DateOnly? DocumentDate { get; private set; }
    public string? Issuer { get; private set; }

    /// <summary>Version 1 for original upload; incremented by Supersede.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>References the document this version supersedes (null for originals).</summary>
    public Guid? SupersedesDocumentId { get; private set; }

    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }

    public string ScanStatus { get; private set; } = DocumentScanStatus.Pending;
    public DateTimeOffset? ScanCompletedAtUtc { get; private set; }

    public DateOnly? RetentionUntil { get; private set; }

    /// <summary>
    /// When true, the document cannot be removed (spec §10.4 invariant 7).
    /// LegalHold is set/cleared by authorized users; default is false.
    /// </summary>
    public bool LegalHold { get; private set; }

    // IHasConcurrencyStamp (ADR-006) — DO NOT mutate manually in domain methods.
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString("N");

    private EvidenceDocument() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new EvidenceDocument in PendingUpload status.
    /// The caller (application layer) is responsible for providing:
    /// - a non-guessable storageObjectName (tenant-partitioned);
    /// - a validated contentType from the EvidenceType allowlist.
    /// </summary>
    public static EvidenceDocument CreateUploadIntent(
        Guid tenantId,
        Guid caseId,
        Guid? claimId,
        string evidenceType,
        string originalFileName,
        string storageObjectName,
        string storageContainer,
        string contentType,
        long sizeBytes,
        DateOnly? documentDate,
        string? issuer,
        Guid uploadedBy,
        TimeProvider timeProvider,
        Guid? id = null)
    {
        if (!Documents.EvidenceType.IsValid(evidenceType))
        {
            throw new ArgumentException($"Unknown evidence type '{evidenceType}'.", nameof(evidenceType));
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(storageObjectName))
        {
            throw new ArgumentException("Storage object name is required.", nameof(storageObjectName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero.", nameof(sizeBytes));
        }

        var now = timeProvider.GetUtcNow();

        return new EvidenceDocument
        {
            Id = id ?? Guid.CreateVersion7(),
            TenantId = tenantId,
            CaseId = caseId,
            ClaimId = claimId,
            EvidenceType = evidenceType,
            Status = DocumentStatus.PendingUpload,
            OriginalFileName = originalFileName.Trim(),
            StorageObjectName = storageObjectName,
            StorageContainer = storageContainer,
            ContentType = contentType.Trim().ToLowerInvariant(),
            SizeBytes = sizeBytes,
            DocumentDate = documentDate,
            Issuer = issuer?.Trim(),
            VersionNumber = 1,
            SupersedesDocumentId = null,
            UploadedBy = uploadedBy,
            UploadedAtUtc = now,
            ScanStatus = DocumentScanStatus.Pending,
            LegalHold = false,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
    }

    /// <summary>
    /// Creates a new EvidenceDocument that supersedes <paramref name="previousDocument"/>.
    /// Transitions previousDocument to Superseded and initializes the new document in
    /// PendingUpload status with VersionNumber incremented by 1 and SupersedesDocumentId linked.
    /// </summary>
    public static Result<EvidenceDocument> CreateSupersedingDocument(
        EvidenceDocument previousDocument,
        Guid? newDocumentId,
        string originalFileName,
        string storageObjectName,
        string storageContainer,
        string contentType,
        long sizeBytes,
        DateOnly? documentDate,
        string? issuer,
        Guid uploadedBy,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(previousDocument);

        var supersedeResult = previousDocument.Supersede();
        if (!supersedeResult.IsSuccess)
        {
            return supersedeResult.Error!;
        }

        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "Original file name is required.");
        }

        if (string.IsNullOrWhiteSpace(storageObjectName))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "Storage object name is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "Content type is required.");
        }

        if (!Documents.EvidenceType.IsMimeTypeAllowed(previousDocument.EvidenceType, contentType))
        {
            return DomainError.Failure(
                "ERR_VALIDATION_FAILED",
                $"Content type '{contentType}' is not allowed for evidence type '{previousDocument.EvidenceType}'.");
        }

        if (sizeBytes <= 0 || sizeBytes > Documents.EvidenceType.MaxFileSizeBytes)
        {
            return DomainError.Failure("ERR_VALIDATION_FAILED", "File size is invalid.");
        }

        var now = timeProvider.GetUtcNow();

        var newDoc = new EvidenceDocument
        {
            Id = newDocumentId ?? Guid.CreateVersion7(),
            TenantId = previousDocument.TenantId,
            CaseId = previousDocument.CaseId,
            ClaimId = previousDocument.ClaimId,
            EvidenceType = previousDocument.EvidenceType,
            Status = DocumentStatus.PendingUpload,
            OriginalFileName = originalFileName.Trim(),
            StorageObjectName = storageObjectName,
            StorageContainer = storageContainer,
            ContentType = contentType.Trim().ToLowerInvariant(),
            SizeBytes = sizeBytes,
            DocumentDate = documentDate,
            Issuer = issuer?.Trim(),
            VersionNumber = previousDocument.VersionNumber + 1,
            SupersedesDocumentId = previousDocument.Id,
            UploadedBy = uploadedBy,
            UploadedAtUtc = now,
            ScanStatus = DocumentScanStatus.Pending,
            LegalHold = false,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };

        return newDoc;
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    /// <summary>
    /// Transitions from PendingUpload → PendingScan.
    /// Called when client reports the upload is complete (spec §16.9 complete-upload).
    /// </summary>
    public Result CompleteUpload(string sha256)
    {
        if (Status != DocumentStatus.PendingUpload)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Document '{Id}' cannot transition to PendingScan from status '{Status}'.");
        }

        if (string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64)
        {
            return DomainError.Failure(
                "ERR_VALIDATION_FAILED",
                "A valid 64-character hex SHA-256 checksum is required.");
        }

        Status = DocumentStatus.PendingScan;
        Sha256 = sha256.ToLowerInvariant().Trim();
        return Result.Success();
    }

    /// <summary>
    /// Transitions from PendingScan → Available (scan returned clean).
    /// Called by DocumentProcessingWorker only (spec §18.1, §10.4 invariant 1).
    /// </summary>
    public Result MarkScanClean(DateTimeOffset scanCompletedAtUtc)
    {
        if (Status != DocumentStatus.PendingScan)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Document '{Id}' cannot be marked clean from status '{Status}'.");
        }

        Status = DocumentStatus.Available;
        ScanStatus = DocumentScanStatus.Clean;
        ScanCompletedAtUtc = scanCompletedAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Transitions from PendingScan → Quarantined (malware detected).
    /// Called by DocumentProcessingWorker. Quarantined documents cannot satisfy
    /// evidence checklists (spec §10.4 invariant 1).
    /// </summary>
    public Result MarkScanMalicious(DateTimeOffset scanCompletedAtUtc)
    {
        if (Status != DocumentStatus.PendingScan)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Document '{Id}' cannot be quarantined from status '{Status}'.");
        }

        Status = DocumentStatus.Quarantined;
        ScanStatus = DocumentScanStatus.Malicious;
        ScanCompletedAtUtc = scanCompletedAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Transitions from PendingScan → Rejected (scanner error / permanent failure).
    /// Called by DocumentProcessingWorker after exhausting retries.
    /// </summary>
    public Result MarkScanFailed(DateTimeOffset scanCompletedAtUtc)
    {
        if (Status != DocumentStatus.PendingScan)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Document '{Id}' cannot be rejected from status '{Status}'.");
        }

        Status = DocumentStatus.Rejected;
        ScanStatus = DocumentScanStatus.Failed;
        ScanCompletedAtUtc = scanCompletedAtUtc;
        return Result.Success();
    }

    /// <summary>
    /// Transitions this document to Superseded when a newer version is uploaded.
    /// The newer version is a separate EvidenceDocument created by the application
    /// layer with SupersedesDocumentId pointing to this document's Id.
    ///
    /// Spec §10.4 invariant 6: superseding does not erase the prior version.
    /// </summary>
    public Result Supersede()
    {
        if (Status != DocumentStatus.Available)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_CANNOT_SUPERSEDE",
                $"Only Available documents can be superseded. Document '{Id}' is '{Status}'.");
        }

        Status = DocumentStatus.Superseded;
        return Result.Success();
    }

    /// <summary>
    /// Transitions to Removed status (soft removal).
    /// Spec §10.4 invariant 7: cannot remove a document under LegalHold.
    /// </summary>
    public Result MarkRemoved()
    {
        if (LegalHold)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_LEGAL_HOLD",
                $"Document '{Id}' is under legal hold and cannot be removed.");
        }

        if (DocumentStatus.IsTerminal(Status))
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Document '{Id}' is in terminal status '{Status}' and cannot be removed.");
        }

        Status = DocumentStatus.Removed;
        return Result.Success();
    }

    /// <summary>
    /// Toggles the LegalHold flag. Requires explicit authorized action.
    /// A document under legal hold cannot be removed (spec §10.4 invariant 7).
    /// </summary>
    public Result ToggleLegalHold(bool legalHold)
    {
        if (DocumentStatus.IsTerminal(Status) && legalHold)
        {
            return DomainError.Failure(
                "ERR_DOCUMENT_INVALID_TRANSITION",
                $"Cannot place a terminal-status document '{Id}' under legal hold.");
        }

        LegalHold = legalHold;
        return Result.Success();
    }

    /// <summary>
    /// Assigns the SHA-256 checksum after verification by the processing worker.
    /// Only called when the worker confirms the checksum against the uploaded object.
    /// </summary>
    public void SetVerifiedSha256(string sha256)
    {
        Sha256 = sha256.ToLowerInvariant().Trim();
    }
}

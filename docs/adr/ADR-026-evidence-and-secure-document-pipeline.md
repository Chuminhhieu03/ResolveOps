# ADR-026 — Evidence and Secure Document Pipeline

**Status:** Accepted
**Date:** 2026-09-17
**Phase:** 9
**Authors:** Coding Agent

---

## Context

Phase 9 requires a secure, tenant-isolated pipeline for uploading, scanning, storing, and
retrieving evidence documents (damage photos, signed PODs, invoices, etc.) required for
carrier claims processing.

Key constraints from the specification (§8.6, §10.4, §13.3, §16.9, §19.6):

1. No permanent public storage URLs may ever be returned by the API.
2. Every document must pass malware scanning before becoming available to business workflows.
3. Storage paths must be non-guessable and tenant-partitioned.
4. SHA-256 checksum must be verified server-side (not just trusted from client).
5. Documents under legal hold cannot be removed.
6. Superseding retains the prior version for audit purposes.
7. Open-source-only stack (ADR-022); no Azure Blob Storage.

---

## Decision

### Object storage: MinIO via AWSSDK.S3

MinIO is the chosen object storage backend (ADR-005). The .NET client library is
`AWSSDK.S3` with `ForcePathStyle = true` (required for MinIO's URL format).

Presigned URLs are used exclusively for upload (PUT) and download (GET):
- Upload URL: 15-minute expiry
- Download URL: 30-minute expiry

The MinIO bucket is private. No `GetObjectUrl` or public ACLs are ever used.

### Storage path schema

```
tenants/{tenantId}/cases/{caseId}/{evidenceType}/{documentId}_{safeFileName}
```

- `documentId` is a UUIDv7 — high entropy, time-ordered, non-guessable.
- `safeFileName` strips path traversal characters and limits to 80 characters.
- Path separators are `/` (S3 key structure, not filesystem paths).

### Malware scanning

The `IMalwareScanner` interface allows pluggable scan implementations:

| Environment | Implementation |
|---|---|
| Development / test | `DevelopmentMalwareScanner` (EICAR detection only) |
| Production (Phase 16) | ClamAV via ICAP adapter (to be implemented) |

**Critical constraint**: the scanner is called **outside any database transaction** to prevent
holding DB locks during network I/O (spec §12.5).

### Document processing worker

`DocumentProcessingWorker` (BackgroundService) polls `PendingScan` documents every 10 seconds.

Per document it:
1. Verifies object exists in MinIO.
2. Streams and computes SHA-256 (verifies against client-supplied checksum).
3. Checks for duplicate checksum within same tenant.
4. Runs malware scanner.
5. Updates status (Available / Quarantined / Rejected).
6. If Available: emits `EvidenceAvailableV1` via outbox.

**Assumption A-021 (deferred)**: queue-based processing via `resolveops.document-processing`
(spec §17.4) is deferred to Phase 16. DB polling is simpler and sufficient for current
throughput requirements.

### Abandoned upload cleanup

`AbandonedUploadCleanupJob` (Quartz.NET, every 6 hours) soft-removes `PendingUpload`
documents older than 24 hours.

**Assumption A-020**: No MinIO object deletion is attempted; the client may never have
uploaded an object. Physical MinIO cleanup deferred to Phase 16.

### Domain state machine

```
PendingUpload → PendingScan   (client: CompleteUpload)
PendingScan   → Available     (worker: MarkScanClean)
PendingScan   → Quarantined   (worker: MarkScanMalicious)
PendingScan   → Rejected      (worker: MarkScanFailed — checksum mismatch, object missing, scan error)
Available     → Superseded    (domain: Supersede — new version uploaded)
Available     → Removed       (domain: MarkRemoved — LegalHold must be false)
PendingUpload → Removed       (cleanup job: AbandonedUploadCleanupJob)
```

### EvidenceRequirement (Assumption A-022)

`EvidenceRequirement` is a write-once policy config entity (no `ConcurrencyStamp`,
no `IAuditableEntity`). Changes to requirements create new records tied to new policy
versions, consistent with the `ExceptionPolicy` pattern.

---

## Consequences

### Positive

- Permanent public blob URLs are structurally impossible — no `GetObjectUrl` method exists
  on `IObjectStorageService`.
- Tenant isolation is enforced at storage path level AND EF Core query filter level.
- Malware scan is decoupled from upload (no request timeout risk for large files).
- EICAR test adapter enables meaningful integration tests without real AV infrastructure.
- `EvidenceAvailableV1` event enables downstream claim readiness recalculation (Phase 10).

### Negative / Trade-offs

- DB polling (assumption A-021) adds up to 10-second latency before scan begins.
  Acceptable for MVP; queue-based processing reduces this in Phase 16.
- Abandoned objects in MinIO accumulate until Phase 16 cleanup; mitigated by 24h soft-removal
  marking preventing further URL generation.
- `DevelopmentMalwareScanner` is not a substitute for real AV in staging/production.

---

## Related

- ADR-005: MinIO for Evidence Object Storage
- ADR-022: Open-Source Infrastructure Strategy
- ADR-006: Optimistic Concurrency via ConcurrencyStamp
- spec §8.6, §10.4, §13.3, §15.9, §16.9, §17.3, §18.1, §19.6

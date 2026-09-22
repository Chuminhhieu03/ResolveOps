# ADR-028 — Claim Approval, Submission, Response, and Appeal Workflow

**Status:** Accepted  
**Date:** 2026-09-21  
**Phase:** 11  
**Authors:** Coding Agent  

---

## Context

Phase 11 implements the long-running carrier claim workflow in ResolveOps, moving claims from draft state through review, submission approval, external carrier submission, carrier response recording (acknowledgements, information requests, decisions, settlement offers), partial approval reconciliation, appeals, and unified timeline auditing.

Key constraints from the specification (§0 Rule 4, §8.8, §8.9, §9.2, §10.5, §11, §15.10, §16.10, §17.3, §26.10, §26.11):

1. **AI Safety Rule**: LLMs and AI are strictly forbidden from approving, rejecting, or executing financial state transitions on claims.
2. **Separation of Duties (§26.10)**: A claim preparer or review requester cannot approve their own claim for submission if the claimed amount meets or exceeds the tenant high-value threshold (default 10,000,000 VND).
3. **Immutable Carrier Response History**: Carrier claim responses are append-only; past responses cannot be updated or deleted.
4. **Partial Approval Reconciliation (§26.11 & Edge Case 16)**: When a carrier partially approves a claim, the approved portion proceeds to settlement while the denied difference (`ClaimedAmount - ApprovedAmount`) is preserved for potential appeal or write-off.
5. **Submission Guardrails (§10.5 Invariant 6 & Edge Case 24)**: Claim submission requires approval, satisfied mandatory evidence, and must not occur past the effective claim deadline.
6. **Unique External Submission Reference (§10.5 Invariant 10)**: External submission reference is enforced unique per carrier per tenant where provided.
7. **Atomic Outbox Integration**: Outbox events `ClaimSubmittedV1` and `ClaimDecisionRecordedV1` are committed in the same database transaction as the aggregate state change.

---

## Decision

### 1. Explicit Domain State Machine
The claim lifecycle transitions are modeled deterministically within the `Claim` aggregate root:
- `Draft` / `EvidencePending` -> `ReadyForReview` (`RequestReview`)
- `ReadyForReview` -> `ApprovedForSubmission` (`ApproveForSubmission`)
- `ReadyForReview` -> `Draft` (`ReturnToDraft`)
- `ApprovedForSubmission` -> `Submitted` (`RecordSubmission`)
- `Submitted` -> `Acknowledged` (`RecordAcknowledgement`)
- `Submitted` / `Acknowledged` / `UnderReview` -> `MoreInformationRequested` (`RecordInformationRequest`)
- `MoreInformationRequested` -> `UnderReview` (`SupplyAdditionalInformation`)
- `Submitted` / `Acknowledged` / `UnderReview` / `MoreInformationRequested` -> `Approved` / `PartiallyApproved` / `Denied` (`RecordDecision`)
- `Denied` / `PartiallyApproved` -> `Appealed` (`Appeal`)
- Non-terminal states -> `Cancelled` (`Cancel`)

### 2. Separation of Duties Policy
In `ApproveForSubmissionHandler`, the tenant high-value threshold is retrieved from `TenantSettings` (or defaults to 10,000,000 VND). If `ClaimedAmount >= threshold`, the domain validates that the approver ID differs from `CreatedBy` and from any user who requested review in `_approvals`. Violations return an explicit 403 Forbidden with code `SEPARATION_OF_DUTIES_VIOLATION`.

### 3. Submission Package Generation
`GetSubmissionPackageHandler` composes a complete payload for submission, including claim header, case metadata, carrier details, loss component items, and short-lived (1-hour) presigned download URLs for all clean, verified evidence documents retrieved via `IObjectStorageService`.

### 4. Background Follow-Up Scanner
`ClaimFollowUpScanJob` runs every 15 minutes via Quartz.NET in `ResolveOps.Worker`. It inspects claims in `Submitted`/`UnderReview` that are approaching or past SLA response timeframes, as well as claims in `MoreInformationRequested`. It generates operational `WorkflowTask` items idempotently.

---

## Consequences

- Financial state transitions are auditable, deterministic, and protected from unauthorized approvals.
- Carrier interaction history is immutable and viewable through a unified chronological timeline API (`GET /api/claims/{claimId}/timeline`).
- Integration consumers receive atomic events (`ClaimSubmittedV1`, `ClaimDecisionRecordedV1`) via the transactional outbox.

# ADR-029 — Financial Recovery and Settlement Reconciliation

**Status:** Accepted  
**Date:** 2026-09-22  
**Phase:** 12  
**Authors:** Coding Agent  

---

## Context

Phase 12 implements the financial recovery, reconciliation, and settlement workflow for claims in ResolveOps (§0 Rule 4, §8.10, §8.11, §9.2, §10.5 Invariants 4, 5, 8, 9, 11, 12, §10.6, §11 Edge Cases 16, 17, 25, §15.10, §16.10, §17.3, §24, §26.10, §26.11).

Key business requirements and constraints:
1. **Reconciliation Without Becoming an Accounting ERP**: ResolveOps tracks recovered cash, credit notes, and balance adjustments against approved claims and links to external finance systems without posting double-entry GL journal entries.
2. **Duplicate Transaction Prevention (§10.5 Invariant 11 & Edge Case 17)**: Duplicate imports of the same external payment or credit note must be strictly prevented via unique database constraints.
3. **Recovery Cap (§10.5 Invariant 5)**: Total recovered amount cannot exceed approved amount without an explicit adjustment transaction.
4. **AI Safety Rule (§0 Rule 4 & Invariant 12)**: AI models and LLMs are strictly forbidden from recording recoveries, writing off balances, or transitioning financial states.
5. **No Unexplained Balances at Closure (§24 DoD & Invariant 9)**: A claim cannot close while an unexplained discrepancy remains between claimed, approved, and recovered amounts. Unrecovered or denied portions must be formally resolved through write-off or appeal.

---

## Decision

### 1. Granular Recovery Transaction Entity (`recovery_transactions`)
Financial recoveries are modeled as append-only `RecoveryTransaction` records linked to the `Claim` aggregate root:
- Transaction types: `Payment` (cash receipt/wire), `CreditNote` (carrier invoice credit), `Adjustment` (authorized debit/credit correction).
- Uniqueness: Enforced via unique database index `uix_recovery_transactions_tenant_ref` on `(tenant_id, external_reference)`. Duplicate attempts return `409 Conflict` with code `DUPLICATE_EXTERNAL_REFERENCE`.
- Numeric precision: Handled with `DECIMAL(19,4)` throughout the domain, database schema, and telemetry.

### 2. State Machine and Financial Transitions
- When carrier approves or partially approves a claim, it moves to `SettlementPending` upon commencement of reconciliation.
- As payments/credit notes arrive:
  - If `RecoveredAmount >= ApprovedAmount` (and `ApprovedAmount > 0`), status transitions to `Paid`.
  - If `RecoveredAmount < ApprovedAmount`, status remains `SettlementPending`.
- Once `Paid` or `Closed`, the claim cannot return to `Draft` (§10.5 Invariant 8).
- `Closed` claims are immutable (§10.5 Invariant 9).

### 3. Write-Off and Reconciliation of Denied Differences
- When a carrier partially approves a claim or denies it in full, the unrecovered exposure (`ClaimedAmount - RecoveredAmount`) must be reconciled.
- The `WriteOff` command permits authorized financial users (`Permissions.CanWriteOffClaim`) to record audited write-offs specifying standardized reason codes:
  - `CarrierInsolvent`, `DisputedUnrecoverable`, `DeMinimisBalance`, `CommercialSettlement`, `Other`.
- Total written-off amount is bounded by remaining unrecovered exposure.

### 4. Strict Closure Preconditions (Definition of Done)
`Claim.Close` requires:
- Claim in `Paid`, `SettlementPending`, or `Denied` status.
- Zero unexplained balance: `ClaimedAmount == RecoveredAmount + WrittenOffAmount`. Any variance between what was originally claimed and what was recovered must be accounted for via authorized write-off before closure is permitted.

### 5. Outbox Integration & Separation of Duties
- Recording a recovery atomically publishes `ClaimRecoveryRecordedV1` via the transactional outbox pattern.
- Role-based permissions (`CanRecordRecovery`, `CanWriteOffClaim`, `CanCloseClaim`) ensure separation of duties across logistics operations, claims specialists, and finance.

---

## Consequences

- Full financial audit trail for all recovered funds and write-offs.
- Inability to accidentally import duplicate bank wires or carrier credit notes.
- Guarantees data integrity by eliminating unexplained financial exposure upon claim closure.
- Clean outbox event distribution for downstream reporting and analytics.

# Domain Glossary

> Source: ResolveOps Master Specification, Section 5.
> This glossary is the authoritative terminology reference for all developers, agents, and stakeholders.

---

| Term | Definition |
|---|---|
| **Shipment** | A movement of goods from an origin to a destination under one business reference. |
| **Shipment Leg** | One segment of a shipment handled by a carrier or transport mode. |
| **Milestone** | A planned or actual operational checkpoint such as pickup, hub arrival, or delivery. |
| **Tracking Event** | An immutable inbound fact reported by a carrier, user, device, or integration. |
| **Canonical Event** | A normalized event expressed in ResolveOps terminology. |
| **Exception** | A condition where actual or predicted shipment behavior violates a policy or expected outcome. |
| **Exception Case** | The auditable work item used to investigate and resolve an exception. |
| **Incident** | General operational occurrence. In this system, a confirmed exception is managed through an Exception Case. |
| **Severity** | Operational importance: Low, Medium, High, or Critical. |
| **Financial Exposure** | Estimated maximum financial impact before a claim decision. |
| **SLA** | Service-level target for acknowledgement, first action, resolution, submission, or another milestone. |
| **SLA Clock** | Stateful time calculation that can run, pause, resume, breach, or complete. |
| **Evidence** | A document, image, data record, or signed statement supporting an investigation or claim. |
| **POD** | Proof of Delivery. |
| **BOL** | Bill of Lading. |
| **Claim** | Formal request to a carrier for compensation or credit. |
| **Claimed Amount** | Amount requested from the carrier. |
| **Approved Amount** | Amount accepted by the carrier. |
| **Recovered Amount** | Amount actually received or credited. |
| **Disposition** | Decision describing how an exception is resolved operationally. |
| **Root Cause** | Classified underlying cause of the exception. |
| **Mitigation** | Action that reduces customer, operational, or financial impact before final resolution. |
| **Carrier Scorecard** | Aggregated performance and financial metrics for a carrier. |
| **Tenant** | An organization whose data and configuration are logically isolated. |
| **Idempotency** | Repeating the same request or message produces no additional business side effect. |
| **Outbox** | Database table containing messages committed with business data and published asynchronously. |
| **Inbox** | Record of consumed messages used to prevent duplicate consumer side effects. |
| **Dead-letter Queue** | Storage for messages that cannot be processed successfully after configured attempts. |
| **Exception Fingerprint** | A deterministic hash derived from `TenantId + ShipmentId + ShipmentLegId + ExceptionType + BusinessKey + PolicyVersion`, used to prevent duplicate active cases. |
| **Money** | A value object combining amount (`decimal`) and ISO 4217 currency code. Float/double are prohibited for monetary values. |
| **TenantContext** | The resolved, trusted tenant identity derived from authentication claims — never from arbitrary request body values. |
| **Claim Readiness** | A deterministic calculation of whether a claim has all mandatory evidence, eligibility, approvals, and a valid deadline. |
| **Policy Version** | An immutable snapshot of a business rule set. Open cases retain their original policy version; new policy versions do not retroactively change existing cases without an explicit migration command. |
| **Quarantine** | State for inbound tracking events that cannot be matched to a known shipment or are otherwise unprocessable. |
| **Integration Event** | A stable, versioned event placed in the outbox and consumed asynchronously by other module consumers. |
| **Domain Event** | An internal fact emitted by an aggregate during a transaction; converted to integration events for cross-module communication. |

---

## Exception Types

| Type | Description |
|---|---|
| **PickupDelay** | Shipment not confirmed as picked up within the configured tolerance after planned pickup time. |
| **InTransitDelay** | Shipment predicted or confirmed to miss a planned transit milestone or delivery commitment. |
| **MissedDelivery** | Delivery attempt failed or delivery not completed by the committed time. |
| **PartialDelivery** | Delivered quantity below expected quantity. |
| **Damage** | Goods or packaging reported as damaged, wet, broken, contaminated, or otherwise unacceptable. |
| **Loss** | Shipment cannot be located after the configured investigation threshold. |
| **MissingOrInvalidDocument** | Required documentation is absent, expired, inconsistent, or associated with the wrong shipment. |

---

## Roles

| Role | Description |
|---|---|
| **TenantAdmin** | Manages tenant settings, users, roles, carriers, policies, integrations, API keys. |
| **OperationsManager** | Monitors workload, SLA, case ageing, financial exposure, carrier performance. Can reassign and escalate. |
| **LogisticsCoordinator** | Daily operational user. Reviews exceptions, contacts carriers, uploads evidence, completes tasks. |
| **ExceptionSpecialist** | Handles high-severity or ambiguous cases. Changes classification, investigates root cause. |
| **ClaimsSpecialist** | Validates eligibility, evidence, deadlines. Submits claims, records carrier responses, tracks recovery. |
| **CustomerService** | Views customer-impacting cases and approved communication notes. |
| **WarehouseOperator** | Provides discrepancy data, quantity confirmation, damage photos, inspection reports. |
| **Finance** | Validates loss amounts, records credit notes or payments, reconciles recoveries. |
| **ReadOnlyAuditor** | View-only access to audit history and case timelines. |
| **CarrierExternal** | External user — views only explicitly shared cases, uploads requested evidence. |

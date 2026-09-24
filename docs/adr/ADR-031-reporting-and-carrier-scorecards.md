# ADR-031 — Reporting, Carrier Scorecards, and Asynchronous Export Architecture

**Status:** Accepted  
**Date:** 2026-09-23  
**Phase:** 14  
**Authors:** Coding Agent  

---

## Context

Phase 14 delivers the reporting and carrier scorecards subsystem for ResolveOps (spec §0 Rule 10, §4.1/4.2, §10.6, §11, §12.2–12.4, §15, §16.12, §17.3/17.4, §18.1, §19.9, §21, §22.7, §24 Phase 14, §25).

Logistics operations managers, claims specialists, coordinators, and finance teams require high-performance analytical views and reports across the lifecycle:
- Real-time operations and claims dashboards.
- Exception ageing distribution across operational buckets (0-24h, 24-48h, 48-72h, 3-7d, >7d).
- SLA compliance and resolution velocity.
- Comprehensive carrier scorecards covering 7 key metrics (shipment count, exception rate, on-time rate, severity distribution, avg response time, claim approval rate, recovery rate).
- Financial recovery and write-off analysis.
- Large asynchronous CSV exports with formula-injection mitigation.

Key architectural and security requirements:
1. **Rule 10 (Transactional Boundaries)**: Database transactions must NEVER be held open across external HTTP, object storage uploads (MinIO), or long-running export streaming operations.
2. **Formula Injection Defense (Spec §11 & §19.9)**: Spreadsheets (Excel, Calc) interpret cells starting with `=`, `+`, `-`, `@`, `\t`, `\r` as formulas or macro/DDE commands. All exported text cells must be sanitized.
3. **IDOR & Multi-Tenant Isolation**: Export downloads must be strictly tenant-isolated and authorized to prevent unauthorized document or report retrieval. Permanent public URLs are prohibited; temporary presigned URLs (15-30 min) must be used.
4. **Analytical Query Performance**: Complex analytical queries and groupings must execute with high efficiency without overhead, utilizing Dapper 2.1.66 with explicit `@TenantId` parameterization alongside EF Core global tenant filters.
5. **Read Model Projections**: Core integration events must be consumed asynchronously to maintain up-to-date daily carrier performance snapshots without burdening operational write transactions.

---

## Decision

### 1. Domain Entities & Storage
- `ExportRequest`: Models asynchronous export jobs with state transitions (`Pending` -> `Processing` -> `Completed` / `Failed`), row counts, file metrics, MinIO container (`exports`), and blob paths. Implements `IAuditableEntity` and `IHasConcurrencyStamp`.
- `CarrierPerformanceSnapshot`: Daily aggregation read model partitioned by `(tenant_id, carrier_id, period_date)` capturing shipment volume, delay rates, exception severities, claim outcomes, and financial recovery amounts.
- Entity constants (`ExportStatus`, `ExportType`) defined as `public static class` with `public const string` constants without enums.

### 2. High-Performance Query Model (Dapper & EF Core)
- For analytical reporting queries (`OperationsDashboard`, `ClaimsDashboard`, `ExceptionAgeing`, `SlaPerformance`, `CarrierScorecards`, `FinancialRecovery`), Dapper (pinned at 2.1.66) is utilized on the active database connection.
- Strict tenant isolation is enforced on every query by filtering on `tenant_id = @TenantId`.

### 3. CSV Formula Injection Neutralization
- Implemented `CsvFormulaEscaper` in `ResolveOps.Application.Reporting`.
- Any text starting with `=`, `+`, `-`, `@`, `\t`, `\r` is prepended with a single quote `'` and quoted according to RFC 4180. Internal quotes are escaped as `""`.

### 4. Asynchronous Export Pipeline, Strategy Pattern (OCP) & MinIO Integration
- Strategy pattern (`IExportDataGenerator`) introduced for Open/Closed Principle compliance:
  - `ExceptionCasesExportGenerator`: Generates formula-safe CSV for exception cases with criteria filtering.
  - `ClaimsExportGenerator`: Generates formula-safe CSV for claims with financial metrics.
  - `CarrierScorecardsExportGenerator`: Generates formula-safe CSV for aggregated carrier scorecards.
- Export Endpoints:
  - `POST /api/exports/exception-cases`
  - `POST /api/exports/claims`
  - `POST /api/exports/carrier-scorecards`
  - `GET /api/exports`: Lists user's recent export requests with pagination, status, and active presigned download URLs. Solves the page refresh (F5) or navigation loss issue.
  - `GET /api/exports/{exportId:guid}`: Returns specific export status with presigned download URL.
- `ExportProcessingJob` in `ResolveOps.Worker`:
  - Dispatches dynamically to the appropriate `IExportDataGenerator` based on `ExportRequest.ExportType`.
  - Streams and uploads CSV to MinIO container `exports` outside database transactions (Rule 10).
  - Automatically records an in-app `Notification` (`NotificationClass.ExportCompleted`) and pushes a real-time SignalR notification (`INotificationRealtimeService`) to the user upon export completion.

### 5. Event-Driven Carrier Scorecard Projections (OCP & Multi-Leg Resolution)
- Strategy pattern (`IReportingEventProjector`) introduced to adhere strictly to OCP:
  - Individual projectors: `ShipmentCreatedProjector`, `TrackingEventAcceptedProjector`, `ExceptionDetectedProjector`, `ClaimSubmittedProjector`, `ClaimDecisionRecordedProjector`, `ClaimRecoveryRecordedProjector`.
  - Adding future event projections requires only implementing `IReportingEventProjector` without mutating the consumer service.
- Multi-Leg Logistics Support:
  - `ShipmentCreatedProjector` records volume for all distinct carriers involved across all legs of the shipment.
  - `ExceptionDetectedProjector` inspects `ExceptionCase.ShipmentLegId` to attribute the exception accurately to the responsible leg carrier rather than defaulting to leg 1.
- `ReportingProjectionConsumerService` in `ResolveOps.Worker`:
  - Listens on `resolveops.reporting` queue bound to monitored integration events.
  - Enforces transactional inbox deduplication (`InboxMessage`).
  - Dispatches to matching `IReportingEventProjector` from the DI container.
  - Updates or initializes daily rollup `CarrierPerformanceSnapshot` records (`PeriodDate`).

### 6. Observability
- Implemented `ReportingMetrics` in `ResolveOps.Observability`:
  - `reporting.queries.duration.seconds` (Histogram<double>)
  - `reporting.exports.total` (Counter<long>)
  - `reporting.exports.duration.seconds` (Histogram<double>)
  - `reporting.projections.processed.total` (Counter<long>)
- Registered `ResolveOps.Reporting` meter and tracing source in `ServiceDefaults`.

---

## Consequences

### Positive
- Heavy analytical reporting queries do not contend with operational write paths.
- Spreadsheets opened by operations or carrier users are protected from formula/DDE injection attacks.
- Rule 10 is strictly preserved — object storage uploads and export streaming occur outside DB transactions.
- Scorecard aggregations are precomputed via event projections, keeping dashboard queries sub-second even with millions of records.
- Pre-signed URLs eliminate unnecessary API data buffering and isolate storage credentials.

### Negative / Trade-offs
- Carrier scorecards reflect near-real-time data updated as events are consumed; live-data fallback ensures instant accuracy when snapshots are not yet populated.
- Quartz job polling interval introduces a minor latency (average ~15 seconds) before large export downloads become ready.

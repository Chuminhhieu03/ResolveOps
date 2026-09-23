# ADR-032 — Frontend Production Workflow and Enterprise Web Application

**Status:** Accepted  
**Date:** 2026-09-23  
**Phase:** 15  
**Authors:** Coding Agent  

---

## Context

Phase 15 delivers the production web frontend for ResolveOps (spec §0, §4.1/4.2, §10.6, §11, §13.4, §16, §19.9, §24 Phase 15, §25, and ADR-021).

Logistics operations teams (Operations Managers, Claims Specialists, Logistics Coordinators, Finance Officers, and System Administrators) require a responsive, high-performance, real-time web portal to manage shipments, triage exceptions, execute claims workflows, inspect evidence, administer policy rules, and monitor carrier performance scorecards.

Key requirements and non-negotiables:
1. **Rule 4 & Human Governance (Spec §0)**: AI is strictly advisory; financial approvals and claim submissions must require explicit human operator confirmation with role-based approval tier thresholds (<$1,000 Logistics Coordinator; <$10,000 Claims Specialist; $10,000+ Operations Manager).
2. **Tenant Invariants (Spec §10.6)**: All requests must convey `X-Tenant-Id` header and JWT bearer token. The application must support active tenant switching with reactive state refresh and tenant-isolated routing.
3. **Optimistic Concurrency (Spec §16, ADR-006)**: When concurrent mutations conflict (HTTP 409), the UI must not corrupt state or crash; it must present a clear resolution dialog allowing the user to refresh the latest server state.
4. **Real-Time Visibility (Spec §13.4, §21, ADR-030)**: Live SignalR notifications (`/hubs/notifications`) for SLA breaches, exception detections, claim state transitions, and task assignments.
5. **Spreadsheet Formula Protection (Spec §11, §19.9)**: Display and export of customer/carrier free-text inputs must neutralize CSV formula injection (`=`, `+`, `-`, `@`).
6. **Enterprise Architecture (ADR-021)**: Built with Angular 19+ standalone components, Angular Material, RxJS, and TypeScript 5.

---

## Decision

### 1. Technology Foundation & Layout Structure
- **Framework:** Angular 19.2 standalone architecture (no `NgModule` ceremony).
- **Component & Design System:** Angular Material 19 with custom enterprise theme:
  - Header: Dark Navy (`#0f172a`) with tenant dropdown, real-time alert badge, and user persona menu.
  - Sidebar: Slate (`#1e293b`) with expandable navigation categorized by operations, recovery, and administration.
  - Body: Modern Slate Canvas (`#f8fafc`) with elevation cards and responsive typography.
- **Routing & Guards:** Lazy-loaded standalone routes guarded by `authGuard` (authentication check) and `roleGuard` (fine-grained RBAC).

### 2. Multi-Tenant Context & Authentication Interceptors
- `TenantService`: Holds current active tenant in browser storage (`resolveops_current_tenant`) and broadcasts changes via `currentTenant$` observable. Switching tenants automatically clears cache and refreshes active views.
- `AuthService`: Manages access tokens and refresh tokens in browser storage (`resolveops_access_token`, `resolveops_refresh_token`). Includes developer persona switcher (`admin`, `ops.manager`, `claims.specialist`, `logistics.coord`, `finance`).
- `AuthInterceptor`: Automatically attaches `Authorization: Bearer <token>` and `X-Tenant-Id: <currentTenantId>` to all outgoing HTTP requests.
- `ErrorInterceptor`:
  - Intercepts `401 Unauthorized` to trigger token refresh or navigate to `/login`.
  - Intercepts `403 Forbidden` to display an error notification.
  - Intercepts `409 Conflict` and triggers `ConcurrencyService.notifyConflict()` to display the concurrency dialog.

### 3. Real-Time Operations via SignalR
- `SignalRNotificationService`: Connects to backend `/hubs/notifications` endpoint with automatic reconnection and JWT token factory.
- Subscribes to real-time events:
  - `NotificationReceived`: Feeds sliding notification drawer and increments top-bar badge.
  - `ExceptionDetected`: Alerts coordinators to urgent shipments requiring triage.
  - `ClaimStatusChanged`: Updates claims specialist workflows.
  - `TaskAssigned`: Notifies assignees of SLA tasks.
  - `ExportReady`: Triggers download toast for completed CSV export jobs.

### 4. 12 Enterprise Operational Screens
1. **Login & Persona Switcher (`/login`):** Standard credentials authentication plus rapid developer persona switcher for simulating Operations Manager, Claims Specialist, Logistics Coordinator, Finance, and Administrator roles.
2. **Operations Dashboard (`/dashboard`):** Real-time operational command center with KPI cards (open exceptions, breached SLAs, active claims, pending recoveries), exception ageing distribution charts, and critical SLA countdown table.
3. **Exception Worklist (`/exceptions`):** Filterable table with severity badges, SLA status indicators, and modal action dialogs for Triage (`TriageDialogComponent`) and Assignment (`AssignDialogComponent`).
4. **Exception Detail (`/exceptions/:id`):** 360-degree case management view with status badges, financial exposure summary, chronological milestone timeline, root cause categorization, linked tasks, and evidence attachments.
5. **Shipment Tracking (`/shipments` & `/shipments/:id`):** Multi-carrier shipment overview, interactive tracking milestone tracker, IoT telemetry sensor monitors (temperature, humidity, shock/impact), and geofence status.
6. **Task Management Queue (`/tasks`):** Operational task queue with priority markers, due-date warning indicators, and inline task completion actions.
7. **Evidence & Document Pipeline (`/evidence`):** Secure file upload zone with drag-and-drop, client-side validation (max 25MB, supported MIME types), virus scan status badges, SHA-256 hash display, and preview drawer.
8. **Claim Preparation Studio (`/claims/prepare`):** Claim drafting studio with automated filing amount calculations, evidence packet completeness checklist, and explicit Rule 4 human governance warning banners.
9. **Claim Detail & Financial Approval (`/claims/:id`):** End-to-end claim resolution lifecycle with carrier response recording, appeal filing, settlement reconciliation, and role-tiered approval buttons enforced by client and server rules.
10. **Carrier Scorecards & Analytics (`/reports/carrier-scorecards`):** High-level carrier benchmark dashboard reporting 7 core operational metrics with date-range filters and safe CSV export downloads.
11. **Tracking Ingestion Quarantine (`/quarantine`):** Ingestion error recovery console showing raw payload viewer, JSON structure inspector, failure diagnostics, and one-click reprocess button.
12. **Policy Engine Administration (`/admin/policies`):** Rule governance center displaying active policies, condition logic inspector, and interactive test simulation drawer (dry-run evaluator).

### 5. Defensive Data Presentation & Concurrency
- `FormulaSafePipe`: Strips and quotes dangerous prefixes (`=`, `+`, `-`, `@`) before rendering customer/carrier data to prevent CSV formula injection.
- `UtcToLocalPipe`: Formats all ISO-8601 UTC server timestamps into user local time with locale support.
- `ConcurrencyDialogComponent`: Pops automatically on HTTP 409 conflict, displaying resource version mismatch and providing a one-click reload button.

---

## Consequences

### Positive
- Unified, responsive Single Page Application providing a cohesive experience across all operational personas.
- Strict multi-tenant isolation enforced consistently at the HTTP transport layer (`X-Tenant-Id`).
- Human operator governance (Rule 4) reinforced visibly in claim preparation and approval workflows.
- Real-time SignalR notifications keep operations coordinators and claims specialists immediately informed of SLA breaches.
- Safe CSV export rendering neutralizes spreadsheet formula injection risks.

### Negative / Trade-offs
- Client-side bundle size is larger than plain HTML/CSS (mitigated by Angular standalone tree-shaking and route lazy-loading).
- SignalR connection requires WebSocket support in corporate proxy configurations (fallback to Long Polling handled automatically by `@microsoft/signalr`).

export interface ExceptionCaseSummary {
  id: string;
  caseNumber: string;
  shipmentId: string;
  shipmentLegId?: string;
  exceptionType: string;
  status: string;
  severity: string;
  severityScore?: number;
  ownerTeamCode?: string;
  financialExposure: number;
  exposureCurrency: string;
  detectedAtUtc: string;
  createdAtUtc: string;
  concurrencyStamp: string;
}

export interface ListExceptionCasesResponse {
  items: ExceptionCaseSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OccurrenceDetail {
  id: string;
  trackingEventId?: string;
  occurrenceType: string;
  observedAtUtc: string;
  summary: string;
  createdAtUtc: string;
}

export interface TimelineEntryDetail {
  id: string;
  entryType: string;
  actorType: string;
  actorId?: string;
  summary: string;
  detailsJson?: string;
  createdAtUtc: string;
  correlationId?: string;
}

export interface ExceptionCaseDetail {
  id: string;
  caseNumber: string;
  shipmentId: string;
  shipmentLegId?: string;
  exceptionType: string;
  fingerprint: string;
  status: string;
  severity: string;
  severityScore?: number;
  policyId: string;
  policyVersionNumber: number;
  ownerUserId?: string;
  ownerTeamCode?: string;
  financialExposure: number;
  exposureCurrency: string;
  rootCauseCode?: string;
  dispositionCode?: string;
  detectedAtUtc: string;
  resolvedAtUtc?: string;
  closedAtUtc?: string;
  createdAtUtc: string;
  concurrencyStamp: string;
  occurrences: OccurrenceDetail[];
  timelineEntries: TimelineEntryDetail[];
}

export interface TriageCaseRequest {
  severity: string;
  rootCauseCode: string;
  assignedTeam?: string;
  assignedUserId?: string;
  note?: string;
  concurrencyStamp: string;
}

export interface AssignCaseRequest {
  assignedUserId?: string;
  assignedTeamCode?: string;
  note?: string;
  concurrencyStamp: string;
}

export interface ResolveCaseRequest {
  resolutionCode: string;
  note: string;
  concurrencyStamp: string;
}

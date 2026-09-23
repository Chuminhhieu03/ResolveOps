export interface QuarantinedEventSummary {
  id: string;
  inboundReceiptId: string;
  reasonCode: string;
  detail: string;
  status: string; // 'Quarantined', 'Reprocessed', 'Resolved', 'DeadLettered'
  assignedUserId?: string;
  resolvedShipmentId?: string;
  resolvedAtUtc?: string;
  createdAtUtc: string;
}

export interface QuarantinedEventDetail extends QuarantinedEventSummary {
  sourceSystem: string;
  externalEventId?: string;
  rawPayload: string;
  receivedAtUtc: string;
}

export interface ListQuarantinedEventsResponse {
  items: QuarantinedEventSummary[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

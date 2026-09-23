export interface LossComponent {
  id?: string;
  componentType: string; // 'ItemCost', 'FreightCharge', 'Labor', 'Customs', 'SalvageCredit'
  description: string;
  quantity?: number;
  unitAmount?: number;
  amount: number;
  currency: string;
  sourceDocumentId?: string;
}

export interface ClaimDetail {
  id: string;
  claimNumber: string;
  caseId: string;
  carrierId: string;
  claimType: string;
  status: string; // 'Draft', 'PendingReview', 'Approved', 'Submitted', 'Acknowledged', 'InformationRequested', 'ApprovedByCarrier', 'RejectedByCarrier', 'Disputed', 'Settled', 'WrittenOff', 'Closed', 'Cancelled'
  eligibilityStatus: string;
  eligibilityReasonCodes: string[];
  claimDeadlineAtUtc?: string;
  claimedAmount: number;
  approvedAmount: number;
  recoveredAmount: number;
  currency: string;
  externalSubmissionReference?: string;
  submittedAtUtc?: string;
  createdAtUtc: string;
  lossComponents: LossComponent[];
}

export interface CreateClaimRequest {
  claimType: string;
  currency: string;
  lossComponents: LossComponent[];
}

export interface ApproveClaimRequest {
  decision: 'Approved' | 'Rejected';
  decisionNotes?: string;
}

export interface RecordSubmissionRequest {
  submissionChannel: string;
  externalReference?: string;
  notes?: string;
}

export interface RecordDecisionRequest {
  outcome: 'Approved' | 'Partial' | 'Rejected';
  approvedAmount: number;
  decisionNotes?: string;
  rejectionReasonCode?: string;
}

export interface RecordRecoveryRequest {
  recoveryType: 'Payment' | 'CreditNote' | 'Setoff';
  amount: number;
  currency: string;
  paymentReference: string;
  notes?: string;
}

export interface WriteOffClaimRequest {
  reason: string;
  notes?: string;
}

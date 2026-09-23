export interface CarrierScorecard {
  carrierId: string;
  carrierName: string;
  shipmentCount: number;
  exceptionRate: number;
  onTimeRate: number;
  severityDistribution: Record<string, number>;
  avgResponseTimeHours: number;
  claimApprovalRate: number;
  recoveryRate: number;
  totalClaimedAmount: number;
  totalApprovedAmount: number;
  totalRecoveredAmount: number;
}

export interface CarrierScorecardsResponse {
  scorecards: CarrierScorecard[];
  periodFrom?: string;
  periodTo?: string;
}

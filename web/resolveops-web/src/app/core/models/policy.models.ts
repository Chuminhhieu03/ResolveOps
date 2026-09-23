export interface ExceptionPolicy {
  id: string;
  policyKey: string;
  versionNumber: number;
  exceptionType: string;
  ruleDefinitionJson: string;
  severityDefinitionJson: string;
  assignmentDefinitionJson: string;
  status: string; // 'Draft', 'Active', 'Retired'
  effectiveFromUtc: string;
  effectiveToUtc?: string;
  evidencePolicyVersionId?: string;
  slaPolicyVersionId?: string;
}

export interface ListPoliciesResponse {
  items: ExceptionPolicy[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SlaPolicySummary {
  id: string;
  policyKey: string;
  name: string;
  acknowledgementMinutes: number;
  resolutionMinutes: number;
  pauseReasonCodes: string[];
}

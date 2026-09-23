export interface WorkflowTask {
  id: string;
  caseId: string;
  claimId?: string;
  taskType: string;
  title: string;
  description?: string;
  status: string; // 'Pending', 'InProgress', 'Completed', 'Blocked', 'Cancelled', 'Waived'
  priority: string; // 'Critical', 'High', 'Medium', 'Low'
  ownerUserId?: string;
  ownerTeamCode?: string;
  dueAtUtc?: string;
  blockedReason?: string;
  completionNote?: string;
  completedAtUtc?: string;
  isMandatory: boolean;
  waivedReason?: string;
  waivedByUserId?: string;
  waivedAtUtc?: string;
  createdAtUtc: string;
  concurrencyStamp: string;
}

export interface CompleteTaskRequest {
  completionNote?: string;
  concurrencyStamp: string;
}

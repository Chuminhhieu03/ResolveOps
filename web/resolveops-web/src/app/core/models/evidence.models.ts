export interface CaseEvidenceSummary {
  id: string;
  evidenceType: string;
  status: string;
  scanStatus: string; // 'Pending', 'Clean', 'Infected'
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  versionNumber: number;
  supersedesDocumentId?: string;
  uploadedBy: string;
  uploadedAtUtc: string;
  scanCompletedAtUtc?: string;
  legalHold: boolean;
}

export interface ListCaseEvidenceResponse {
  items: CaseEvidenceSummary[];
}

export interface CreateUploadIntentRequest {
  evidenceType: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  sha256?: string;
  carrierId?: string;
}

export interface CreateUploadIntentResponse {
  documentId: string;
  uploadMethod: string;
  uploadUrl: string;
  expiresAt: string;
  storageContainer: string;
  storageObjectName: string;
}

export interface CompleteUploadRequest {
  sha256: string;
}

export interface DownloadIntentResponse {
  downloadUrl: string;
  expiresAt: string;
}

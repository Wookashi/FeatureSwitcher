export interface PendingFeatureDto {
  applicationName: string;
  featureName: string;
  lastUsedAt: string;
  pendingDeletionSince: string;
}

export interface PendingApplicationDto {
  applicationName: string;
  lastUsedAt: string;
  pendingDeletionSince: string;
}

export interface PendingFeatureItem extends PendingFeatureDto {
  nodeId: number;
  nodeName: string;
}

export interface PendingApplicationItem extends PendingApplicationDto {
  nodeId: number;
  nodeName: string;
}

export interface DeletionFailure {
  nodeName: string;
  applicationName: string;
  featureName?: string; // undefined when the failure is for an application-level delete
  reason: string;       // 'restored' | 'not-found' | 'error'
  message?: string;
}

export interface DeletionSummary {
  successCount: number;
  failures: DeletionFailure[];
}

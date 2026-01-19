// API DTOs
export interface NodeDto {
  id: number;
  name: string;
  address: string;
}

export interface ApplicationDto {
  name: string;
  environment: string;
}

export interface FeatureDto {
  name: string;
  state: boolean;
}

// Cell state for the matrix
export type CellState =
  | { kind: 'loading' }
  | { kind: 'value'; value: boolean }
  | { kind: 'unknown'; reason?: string };

// Row in the feature matrix table
export interface FeatureMatrixRow {
  key: string; // `${application}::${feature}`
  application: string;
  feature: string;
  cells: Record<string, CellState>; // nodeName -> CellState
}

// Error entry for the error panel
export interface FetchError {
  id: string;
  timestamp: Date;
  endpoint: string;
  message: string;
}

// State of the feature matrix
export interface FeatureMatrixState {
  nodes: NodeDto[];
  rows: FeatureMatrixRow[];
  errors: FetchError[];
  isLoadingNodes: boolean;
  loadingApplications: Set<string>; // nodeName
  loadingFeatures: Set<string>; // `${nodeName}::${applicationName}`
}

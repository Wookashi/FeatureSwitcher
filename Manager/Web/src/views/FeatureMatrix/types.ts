// API DTOs
export interface NodeDto {
  id: number;
  name: string;
  address: string;
  role: string;
}

export interface ApplicationDto {
  name: string;
  environment: string;
}

export interface FeatureLink {
  url: string;
  title?: string | null; // without it the UI shows host + path
}

export interface FeatureDto {
  name: string;
  state: boolean;
  lastUsedAt?: string;
  usesLast7Days?: number;
  // Details registered by the application; absent from older nodes.
  description?: string | null;
  links?: FeatureLink[];
}

export interface NodeStateDto {
  status: string;
  version?: string | null;
}

// Cell state for the matrix
export interface CellUsage {
  lastUsedAt?: string;
  usesLast7Days?: number;
}

// Details as registered by the application on this node (they may differ between nodes
// running different versions of the application).
export interface FeatureDetails {
  description?: string | null;
  links: FeatureLink[];
}

export type CellState =
  | { kind: 'loading' }
  | ({ kind: 'value'; value: boolean; details?: FeatureDetails } & CellUsage)
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

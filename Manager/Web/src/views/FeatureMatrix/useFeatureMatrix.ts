import { useState, useCallback, useRef, useEffect } from 'react';
import type {
  NodeDto,
  ApplicationDto,
  FeatureDto,
  FeatureMatrixRow,
  FetchError,
  CellState,
} from './types';
import { ConcurrencyLimiter } from './concurrency';

interface UseFeatureMatrixResult {
  nodes: NodeDto[];
  rows: FeatureMatrixRow[];
  errors: FetchError[];
  unreachableNodes: Map<string, string>; // nodeName -> error message
  isLoadingNodes: boolean;
  isLoading: boolean;
  refresh: () => void;
  toggleFeatureState: (nodeId: number, nodeName: string, application: string, feature: string, currentValue: boolean) => Promise<void>;
}

function generateErrorId(): string {
  return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
}

function makeRowKey(application: string, feature: string): string {
  return `${application}::${feature}`;
}

export function useFeatureMatrix(): UseFeatureMatrixResult {
  const [nodes, setNodes] = useState<NodeDto[]>([]);
  const [rows, setRows] = useState<FeatureMatrixRow[]>([]);
  const [errors, setErrors] = useState<FetchError[]>([]);
  const [unreachableNodes, setUnreachableNodes] = useState<Map<string, string>>(new Map());
  const [isLoadingNodes, setIsLoadingNodes] = useState(true);
  const [pendingRequests, setPendingRequests] = useState(0);

  const abortControllerRef = useRef<AbortController | null>(null);
  const limiterRef = useRef<ConcurrencyLimiter>(new ConcurrencyLimiter(6));

  // Cache: nodeName -> applicationName -> Map(featureName -> boolean)
  const cacheRef = useRef<Map<string, Map<string, Map<string, boolean>>>>(new Map());
  // All known features per application: applicationName -> Set(featureNames)
  const appFeaturesRef = useRef<Map<string, Set<string>>>(new Map());

  const addError = useCallback((endpoint: string, message: string) => {
    const error: FetchError = {
      id: generateErrorId(),
      timestamp: new Date(),
      endpoint,
      message,
    };
    setErrors((prev) => [...prev, error]);
  }, []);

  const updateRowCell = useCallback(
    (application: string, feature: string, nodeName: string, cellState: CellState) => {
      setRows((prevRows) => {
        const key = makeRowKey(application, feature);
        const existingIndex = prevRows.findIndex((r) => r.key === key);

        if (existingIndex >= 0) {
          const updated = [...prevRows];
          updated[existingIndex] = {
            ...updated[existingIndex],
            cells: {
              ...updated[existingIndex].cells,
              [nodeName]: cellState,
            },
          };
          return updated;
        } else {
          // Create new row
          const newRow: FeatureMatrixRow = {
            key,
            application,
            feature,
            cells: { [nodeName]: cellState },
          };
          return [...prevRows, newRow].sort((a, b) => {
            const appCmp = a.application.localeCompare(b.application);
            if (appCmp !== 0) return appCmp;
            return a.feature.localeCompare(b.feature);
          });
        }
      });
    },
    []
  );

  const fetchFeatures = useCallback(
    async (nodeId: number, nodeName: string, applicationName: string, signal: AbortSignal) => {
      const endpoint = `/api/nodes/${nodeId}/applications/${encodeURIComponent(applicationName)}/features`;

      try {
        setPendingRequests((n) => n + 1);
        const timeoutSignal = AbortSignal.timeout(10000); // 10 second timeout
        const combinedSignal = AbortSignal.any([signal, timeoutSignal]);
        const response = await fetch(endpoint, { signal: combinedSignal });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        const features: FeatureDto[] = await response.json();

        // Update cache
        if (!cacheRef.current.has(nodeName)) {
          cacheRef.current.set(nodeName, new Map());
        }
        const nodeCache = cacheRef.current.get(nodeName)!;
        const appCache = new Map<string, boolean>();
        features.forEach((f) => appCache.set(f.name, f.state));
        nodeCache.set(applicationName, appCache);

        // Update known features for this application
        if (!appFeaturesRef.current.has(applicationName)) {
          appFeaturesRef.current.set(applicationName, new Set());
        }
        const knownFeatures = appFeaturesRef.current.get(applicationName)!;
        features.forEach((f) => knownFeatures.add(f.name));

        // Update cells for features we received
        features.forEach((f) => {
          updateRowCell(applicationName, f.name, nodeName, { kind: 'value', value: f.state });
        });

        // For features known from other nodes but missing here, mark as unknown
        knownFeatures.forEach((featureName) => {
          if (!appCache.has(featureName)) {
            updateRowCell(applicationName, featureName, nodeName, {
              kind: 'unknown',
              reason: 'Feature not present on this node',
            });
          }
        });
      } catch (err) {
        if (signal.aborted) return;

        const message = err instanceof Error ? err.message : 'Unknown error';
        addError(endpoint, message);

        // Mark all known features for this app as unknown for this node
        const knownFeatures = appFeaturesRef.current.get(applicationName);
        if (knownFeatures) {
          knownFeatures.forEach((featureName) => {
            updateRowCell(applicationName, featureName, nodeName, {
              kind: 'unknown',
              reason: message,
            });
          });
        }
      } finally {
        setPendingRequests((n) => n - 1);
      }
    },
    [addError, updateRowCell]
  );

  const fetchApplications = useCallback(
    async (node: NodeDto, signal: AbortSignal) => {
      const endpoint = `/api/nodes/${node.id}/applications`;

      try {
        const timeoutSignal = AbortSignal.timeout(10000); // 10 second timeout
        const combinedSignal = AbortSignal.any([signal, timeoutSignal]);
        const response = await fetch(endpoint, { signal: combinedSignal });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        const applications: ApplicationDto[] = await response.json();

        // Node responded successfully - mark as reachable
        setUnreachableNodes((prev) => {
          if (!prev.has(node.name)) return prev;
          const next = new Map(prev);
          next.delete(node.name);
          return next;
        });

        // Queue feature fetches for each application
        applications.forEach((app) => {
          limiterRef.current.run(() => fetchFeatures(node.id, node.name, app.name, signal));
        });
      } catch (err) {
        if (signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Unknown error';
        addError(endpoint, message);

        // Mark node as unreachable
        setUnreachableNodes((prev) => {
          const next = new Map(prev);
          next.set(node.name, message);
          return next;
        });
      }
    },
    [addError, fetchFeatures]
  );

  const fetchNodes = useCallback(
    async (signal: AbortSignal) => {
      const endpoint = '/api/nodes';

      try {
        const timeoutSignal = AbortSignal.timeout(10000); // 10 second timeout
        const combinedSignal = AbortSignal.any([signal, timeoutSignal]);
        const response = await fetch(endpoint, { signal: combinedSignal });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        const nodeList: NodeDto[] = await response.json();
        setNodes(nodeList);
        setIsLoadingNodes(false);

        // Start fetching applications for each node
        nodeList.forEach((node) => {
          fetchApplications(node, signal);
        });
      } catch (err) {
        if (signal.aborted) return;
        const message = err instanceof Error ? err.message : 'Unknown error';
        addError(endpoint, message);
        setIsLoadingNodes(false);
      }
    },
    [addError, fetchApplications]
  );

  const refresh = useCallback(() => {
    // Abort previous requests
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    limiterRef.current.clear();
    limiterRef.current = new ConcurrencyLimiter(6);

    // Clear state
    cacheRef.current.clear();
    appFeaturesRef.current.clear();
    setNodes([]);
    setRows([]);
    setErrors([]);
    setUnreachableNodes(new Map());
    setIsLoadingNodes(true);
    setPendingRequests(0);

    // Create new abort controller
    const controller = new AbortController();
    abortControllerRef.current = controller;

    fetchNodes(controller.signal);
  }, [fetchNodes]);

  const toggleFeatureState = useCallback(
    async (nodeId: number, nodeName: string, application: string, feature: string, currentValue: boolean) => {
      const newValue = !currentValue;
      const endpoint = `/api/nodes/${nodeId}/applications/${encodeURIComponent(application)}/features/${encodeURIComponent(feature)}`;

      // Set cell to loading
      updateRowCell(application, feature, nodeName, { kind: 'loading' });

      try {
        const response = await fetch(endpoint, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ state: newValue }),
          signal: AbortSignal.timeout(10000), // 10 second timeout
        });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        // Update cache
        const nodeCache = cacheRef.current.get(nodeName);
        if (nodeCache) {
          const appCache = nodeCache.get(application);
          if (appCache) {
            appCache.set(feature, newValue);
          }
        }

        // Update cell with new value
        updateRowCell(application, feature, nodeName, { kind: 'value', value: newValue });
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Unknown error';
        addError(endpoint, message);
        // Mark as unknown on error - we can't confirm the current state
        updateRowCell(application, feature, nodeName, { kind: 'unknown', reason: message });
      }
    },
    [addError, updateRowCell]
  );

  // Initial fetch
  useEffect(() => {
    refresh();

    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      limiterRef.current.clear();
    };
  }, [refresh]);

  return {
    nodes,
    rows,
    errors,
    unreachableNodes,
    isLoadingNodes,
    isLoading: isLoadingNodes || pendingRequests > 0,
    refresh,
    toggleFeatureState,
  };
}

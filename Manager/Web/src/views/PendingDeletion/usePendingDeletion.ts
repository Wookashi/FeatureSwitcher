import { useCallback, useEffect, useRef, useState } from 'react';
import { authFetch } from '../../auth';
import type {
  DeletionFailure,
  DeletionSummary,
  PendingApplicationDto,
  PendingApplicationItem,
  PendingFeatureDto,
  PendingFeatureItem,
} from './types';

interface NodeRef {
  id: number;
  name: string;
}

interface FetchPendingResult {
  features: PendingFeatureItem[];
  applications: PendingApplicationItem[];
}

// The Node sweep that grows this list runs at most once per day (configurable, default 24h).
// Polling more often than the sweep cadence wastes work — there's nothing new to find. We refetch
// on mount, when the modal opens, and after every delete batch, so the badge is always fresh in
// response to user actions. The hourly interval is just a defensive backstop for long-running
// browser sessions that outlast a sweep.
const POLL_INTERVAL_MS = 60 * 60 * 1000;

async function fetchPendingForNode(node: NodeRef, signal: AbortSignal): Promise<FetchPendingResult> {
  const headers = { Accept: 'application/json' };
  const empty: FetchPendingResult = { features: [], applications: [] };

  const [featuresResp, appsResp] = await Promise.all([
    authFetch(`/api/nodes/${node.id}/pending-deletion/features`, { headers, signal }),
    authFetch(`/api/nodes/${node.id}/pending-deletion/applications`, { headers, signal }),
  ]);

  // 403 = user cannot manage pending deletes on this node (non-Admin). Ignore silently.
  if (featuresResp.status === 403 && appsResp.status === 403) {
    return empty;
  }

  if (!featuresResp.ok || !appsResp.ok) {
    return empty;
  }

  const features: PendingFeatureDto[] = await featuresResp.json();
  const applications: PendingApplicationDto[] = await appsResp.json();

  return {
    features: features.map((f) => ({ ...f, nodeId: node.id, nodeName: node.name })),
    applications: applications.map((a) => ({ ...a, nodeId: node.id, nodeName: node.name })),
  };
}

export function usePendingDeletion(enabled: boolean) {
  const [features, setFeatures] = useState<PendingFeatureItem[]>([]);
  const [applications, setApplications] = useState<PendingApplicationItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const abortControllerRef = useRef<AbortController | null>(null);

  const refresh = useCallback(async () => {
    abortControllerRef.current?.abort();
    const controller = new AbortController();
    abortControllerRef.current = controller;

    setIsLoading(true);
    setError(null);

    try {
      const nodesResp = await authFetch('/api/nodes', { signal: controller.signal });
      if (!nodesResp.ok) {
        throw new Error(`Failed to list nodes (HTTP ${nodesResp.status})`);
      }
      const nodes: NodeRef[] = await nodesResp.json();

      const results = await Promise.all(
        nodes.map((node) => fetchPendingForNode(node, controller.signal))
      );

      const allFeatures = results.flatMap((r) => r.features);
      const allApps = results.flatMap((r) => r.applications);

      setFeatures(allFeatures);
      setApplications(allApps);
    } catch (err) {
      if (err instanceof DOMException && err.name === 'AbortError') {
        return;
      }
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Poll
  useEffect(() => {
    if (!enabled) {
      return;
    }
    void refresh();
    const id = window.setInterval(() => { void refresh(); }, POLL_INTERVAL_MS);
    return () => {
      window.clearInterval(id);
      abortControllerRef.current?.abort();
    };
  }, [enabled, refresh]);

  const deleteFeature = useCallback(
    async (nodeId: number, applicationName: string, featureName: string): Promise<DeletionFailure | null> => {
      const url = `/api/nodes/${nodeId}/applications/${encodeURIComponent(applicationName)}/features/${encodeURIComponent(featureName)}/pending`;
      const resp = await authFetch(url, { method: 'DELETE' });
      if (resp.ok) return null;

      if (resp.status === 409) {
        return {
          nodeName: '', applicationName, featureName,
          reason: 'restored',
          message: 'Feature was restored by a recent use',
        };
      }
      if (resp.status === 404) {
        return { nodeName: '', applicationName, featureName, reason: 'not-found' };
      }
      return {
        nodeName: '', applicationName, featureName, reason: 'error',
        message: `HTTP ${resp.status}`,
      };
    },
    []
  );

  const deleteApplication = useCallback(
    async (nodeId: number, applicationName: string): Promise<DeletionFailure | null> => {
      const url = `/api/nodes/${nodeId}/applications/${encodeURIComponent(applicationName)}/pending`;
      const resp = await authFetch(url, { method: 'DELETE' });
      if (resp.ok) return null;

      if (resp.status === 409) {
        return { nodeName: '', applicationName, reason: 'restored', message: 'Application was restored by a recent use' };
      }
      if (resp.status === 404) {
        return { nodeName: '', applicationName, reason: 'not-found' };
      }
      return { nodeName: '', applicationName, reason: 'error', message: `HTTP ${resp.status}` };
    },
    []
  );

  const deleteBatch = useCallback(
    async (
      selectedFeatures: PendingFeatureItem[],
      selectedApplications: PendingApplicationItem[]
    ): Promise<DeletionSummary> => {
      const failures: DeletionFailure[] = [];
      let success = 0;

      for (const f of selectedFeatures) {
        const failure = await deleteFeature(f.nodeId, f.applicationName, f.featureName);
        if (failure) {
          failures.push({ ...failure, nodeName: f.nodeName });
        } else {
          success++;
        }
      }

      for (const a of selectedApplications) {
        const failure = await deleteApplication(a.nodeId, a.applicationName);
        if (failure) {
          failures.push({ ...failure, nodeName: a.nodeName });
        } else {
          success++;
        }
      }

      await refresh();
      return { successCount: success, failures };
    },
    [deleteFeature, deleteApplication, refresh]
  );

  const totalCount = features.length + applications.length;

  return {
    features,
    applications,
    totalCount,
    isLoading,
    error,
    refresh,
    deleteBatch,
  };
}

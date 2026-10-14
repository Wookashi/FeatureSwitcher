import { useEffect, useState } from 'react';
import { authFetch } from '../../auth';

export interface UiSettings {
  // null = unread warning disabled in the Manager configuration (FeatureUsage__UnreadWarningAfter)
  unreadWarningAfterDays: number | null;
}

/**
 * Settings configured on the Manager server. Until they load (or if loading fails) the unread warning
 * is off, so the matrix never shows warnings based on a guessed threshold.
 */
export function useUiSettings(): UiSettings {
  const [settings, setSettings] = useState<UiSettings>({ unreadWarningAfterDays: null });

  useEffect(() => {
    const controller = new AbortController();
    authFetch('/api/ui-settings', { signal: controller.signal })
      .then((response) => (response.ok ? response.json() : null))
      .then((data) => {
        if (data && (typeof data.unreadWarningAfterDays === 'number' || data.unreadWarningAfterDays === null)) {
          setSettings({ unreadWarningAfterDays: data.unreadWarningAfterDays });
        }
      })
      .catch(() => {
        // Keep the safe default; the matrix works without the warning.
      });
    return () => controller.abort();
  }, []);

  return settings;
}

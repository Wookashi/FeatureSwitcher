import { useEffect, useState } from 'react';

let cached: string | null = null;
let inFlight: Promise<string> | null = null;

async function loadVersion(): Promise<string> {
  if (cached) return cached;
  if (inFlight) return inFlight;
  inFlight = fetch('/health', { signal: AbortSignal.timeout(5000) })
    .then((r) => (r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`))))
    .then((data) => {
      const v = typeof data?.version === 'string' ? data.version : 'unknown';
      cached = v;
      return v;
    })
    .catch(() => {
      const v = 'unknown';
      cached = v;
      return v;
    })
    .finally(() => {
      inFlight = null;
    });
  return inFlight;
}

export function useAppVersion(): string | null {
  const [version, setVersion] = useState<string | null>(cached);
  useEffect(() => {
    if (cached) return;
    let alive = true;
    loadVersion().then((v) => {
      if (alive) setVersion(v);
    });
    return () => {
      alive = false;
    };
  }, []);
  return version;
}

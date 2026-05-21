import { useCallback, useEffect, useMemo, useState } from 'react';

const STORAGE_KEY = 'featureMatrix.nodeColumnOrder';

function readStored(): string[] {
  if (typeof window === 'undefined') return [];
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed) && parsed.every((v) => typeof v === 'string')) {
      return parsed;
    }
    return [];
  } catch {
    return [];
  }
}

export interface ColumnOrderApi {
  orderedNames: string[];
  moveLeft: (name: string) => void;
  moveRight: (name: string) => void;
  canMoveLeft: (name: string) => boolean;
  canMoveRight: (name: string) => boolean;
}

export function useColumnOrder(nodeNames: string[]): ColumnOrderApi {
  const [order, setOrder] = useState<string[]>(readStored);

  const orderedNames = useMemo(() => {
    const known = new Set(nodeNames);
    const stored = order.filter((n) => known.has(n));
    const appended = nodeNames.filter((n) => !stored.includes(n));
    return [...stored, ...appended];
  }, [order, nodeNames]);

  useEffect(() => {
    if (orderedNames.length === 0) return;
    const same =
      orderedNames.length === order.length &&
      orderedNames.every((n, i) => order[i] === n);
    if (!same) {
      setOrder(orderedNames);
    }
  }, [orderedNames, order]);

  useEffect(() => {
    if (typeof window === 'undefined') return;
    localStorage.setItem(STORAGE_KEY, JSON.stringify(order));
  }, [order]);

  const swap = useCallback((name: string, delta: -1 | 1) => {
    setOrder((prev) => {
      const idx = prev.indexOf(name);
      const target = idx + delta;
      if (idx < 0 || target < 0 || target >= prev.length) return prev;
      const next = prev.slice();
      next[idx] = prev[target];
      next[target] = name;
      return next;
    });
  }, []);

  const moveLeft = useCallback((name: string) => swap(name, -1), [swap]);
  const moveRight = useCallback((name: string) => swap(name, 1), [swap]);

  const canMoveLeft = useCallback(
    (name: string) => orderedNames.indexOf(name) > 0,
    [orderedNames]
  );
  const canMoveRight = useCallback(
    (name: string) =>
      orderedNames.indexOf(name) >= 0 &&
      orderedNames.indexOf(name) < orderedNames.length - 1,
    [orderedNames]
  );

  return { orderedNames, moveLeft, moveRight, canMoveLeft, canMoveRight };
}

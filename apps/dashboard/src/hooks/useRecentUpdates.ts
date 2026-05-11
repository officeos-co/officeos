"use client";

import { useEffect, useRef, useState } from "react";

export function useRecentUpdates<T>(
  items: T[],
  getId: (item: T) => string,
  getSignature: (item: T) => string,
  {
    durationMs = 1800,
    includeNew = false,
  }: { durationMs?: number; includeNew?: boolean } = {},
): Set<string> {
  const previousRef = useRef<Map<string, string>>(new Map());
  const initializedRef = useRef(false);
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(
    new Map(),
  );
  const [updatedIds, setUpdatedIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    const previous = previousRef.current;
    const next = new Map<string, string>();
    const changedIds: string[] = [];

    for (const item of items) {
      const id = getId(item);
      const signature = getSignature(item);
      const priorSignature = previous.get(id);
      next.set(id, signature);

      if (!initializedRef.current) continue;
      if (priorSignature === undefined ? includeNew : priorSignature !== signature) {
        changedIds.push(id);
      }
    }

    previousRef.current = next;
    initializedRef.current = true;

    if (changedIds.length === 0) return;

    const addTimer = setTimeout(() => {
      setUpdatedIds((current) => {
        const nextIds = new Set(current);
        for (const id of changedIds) nextIds.add(id);
        return nextIds;
      });

      for (const id of changedIds) {
        const existingTimer = timersRef.current.get(id);
        if (existingTimer) clearTimeout(existingTimer);
        const timer = setTimeout(() => {
          setUpdatedIds((current) => {
            const nextIds = new Set(current);
            nextIds.delete(id);
            return nextIds;
          });
          timersRef.current.delete(id);
        }, durationMs);
        timersRef.current.set(id, timer);
      }
    }, 0);

    return () => clearTimeout(addTimer);
  }, [items, getId, getSignature, durationMs, includeNew]);

  useEffect(() => {
    const timers = timersRef.current;
    return () => {
      for (const timer of timers.values()) clearTimeout(timer);
      timers.clear();
    };
  }, []);

  return updatedIds;
}

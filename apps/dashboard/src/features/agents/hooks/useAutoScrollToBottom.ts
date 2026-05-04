import { useLayoutEffect, useRef } from "react";

export function useAutoScrollToBottom<TElement extends HTMLElement>({
  rowCount,
  resetKey,
}: {
  rowCount: number;
  resetKey?: string;
}) {
  const scrollRef = useRef<TElement | null>(null);

  useLayoutEffect(() => {
    const element = scrollRef.current;
    if (!element) return;

    element.scrollTop = element.scrollHeight;
  }, [rowCount, resetKey]);

  return scrollRef;
}

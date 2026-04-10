"use client";

import { useEffect } from "react";

export default function RootError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Unhandled error:", error);
  }, [error]);

  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 p-8">
      <h2 className="text-lg font-medium text-red-300">Something went wrong</h2>
      <p className="max-w-md text-center text-sm text-[var(--eaos-text-muted)]">
        {error.message}
      </p>
      <button
        onClick={reset}
        className="rounded-md border border-[var(--eaos-border)] px-4 py-2 text-sm hover:bg-white hover:text-black"
      >
        Try again
      </button>
    </div>
  );
}

"use client";

import Link from "next/link";
import { useEffect } from "react";

export default function AgentDetailError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Agent detail error:", error);
  }, [error]);

  return (
    <div className="flex h-full flex-col items-center justify-center gap-4 p-8">
      <h2 className="text-lg font-medium text-red-300">Something went wrong</h2>
      <p className="max-w-md text-center text-sm text-muted-foreground">
        {error.message}
      </p>
      <div className="flex gap-2">
        <button
          onClick={reset}
          className="rounded-md border border-border px-4 py-2 text-sm hover:bg-primary hover:text-primary-foreground"
        >
          Try again
        </button>
        <Link
          href="/agents"
          className="rounded-md border border-border px-4 py-2 text-sm hover:bg-primary hover:text-primary-foreground"
        >
          Back to agents
        </Link>
      </div>
    </div>
  );
}

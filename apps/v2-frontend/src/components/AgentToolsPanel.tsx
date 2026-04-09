"use client";

import { useEffect, useState } from "react";
import { Wrench, RefreshCw } from "lucide-react";
import type { Agent } from "@/hooks/useAgents";
import { agentFetch } from "@/lib/agentProxy";

type ToolSpec = {
  name: string;
  description: string;
  parameters: unknown;
};

type ToolsResponse = ToolSpec[] | { tools: ToolSpec[] };

function unwrap(data: ToolsResponse): ToolSpec[] {
  if (Array.isArray(data)) return data;
  return data.tools ?? [];
}

export function AgentToolsPanel({ agent }: { agent: Agent }) {
  const [tools, setTools] = useState<ToolSpec[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<string | null>(null);

  const load = async () => {
    setError(null);
    try {
      const data = await agentFetch<ToolsResponse>(agent.id, "/api/tools");
      const list = unwrap(data);
      list.sort((a, b) => a.name.localeCompare(b.name));
      setTools(list);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tools");
      setTools([]);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [agent.id]);

  if (tools === null) {
    return <div className="mx-8 my-6 text-sm text-[var(--eaos-text-muted)]">Loading tools…</div>;
  }

  return (
    <div className="mx-8 my-6">
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm text-[var(--eaos-text-muted)]">
          <Wrench className="h-4 w-4" />
          {tools.length} tool{tools.length === 1 ? "" : "s"} registered
        </div>
        <button
          type="button"
          onClick={load}
          className="flex items-center gap-1 rounded-md border border-[var(--eaos-border)] px-3 py-1.5 text-xs hover:bg-black/40"
        >
          <RefreshCw className="h-3 w-3" />
          Refresh
        </button>
      </div>

      {error && (
        <div className="mb-3 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-2 text-xs text-red-300">
          {error}
        </div>
      )}

      {tools.length === 0 ? (
        <div className="rounded-xl border border-dashed border-[var(--eaos-border)] bg-[var(--eaos-panel)] px-6 py-12 text-center text-sm text-[var(--eaos-text-muted)]">
          No tools available.
        </div>
      ) : (
        <div className="space-y-2">
          {tools.map((tool) => {
            const isOpen = expanded === tool.name;
            return (
              <div
                key={tool.name}
                className="rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]"
              >
                <button
                  type="button"
                  onClick={() => setExpanded(isOpen ? null : tool.name)}
                  className="flex w-full items-start justify-between gap-3 px-4 py-3 text-left hover:bg-black/30"
                >
                  <div className="min-w-0 flex-1">
                    <div className="font-mono text-sm">{tool.name}</div>
                    <div className="mt-0.5 text-xs text-[var(--eaos-text-muted)]">
                      {tool.description || "(no description)"}
                    </div>
                  </div>
                  <span className="flex-shrink-0 text-[var(--eaos-text-muted)]">
                    {isOpen ? "−" : "+"}
                  </span>
                </button>
                {isOpen && tool.parameters !== undefined && (
                  <pre className="max-h-60 overflow-auto border-t border-[var(--eaos-border)] bg-black/20 p-3 font-mono text-[11px] text-[var(--eaos-text-muted)]">
                    {JSON.stringify(tool.parameters, null, 2)}
                  </pre>
                )}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

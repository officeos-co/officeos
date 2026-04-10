"use client";

import { Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import type { Agent } from "../hooks/useAgents";
import { StatusBadge } from "./StatusBadge";
import { formatDate, shortId } from "../utils/format";

type AgentsTableProps = {
  agents: Agent[];
  onDelete: (id: string) => void;
};

export function AgentsTable({ agents, onDelete }: AgentsTableProps) {
  const router = useRouter();

  return (
    <div className="mx-8 my-6 overflow-hidden rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <table className="w-full text-sm">
        <thead className="text-left text-[var(--eaos-text-muted)]">
          <tr className="border-b border-[var(--eaos-border)]">
            <th className="px-4 py-3 font-normal">ID</th>
            <th className="px-4 py-3 font-normal">Name</th>
            <th className="px-4 py-3 font-normal">Provider</th>
            <th className="px-4 py-3 font-normal">Model</th>
            <th className="px-4 py-3 font-normal">Status</th>
            <th className="px-4 py-3 font-normal">Created</th>
            <th className="w-12 px-4 py-3 font-normal" />
          </tr>
        </thead>
        <tbody>
          {agents.map((agent) => (
            <tr
              key={agent.id}
              onClick={() => router.push(`/agents/${agent.id}`)}
              className="cursor-pointer border-b border-[var(--eaos-border)] last:border-b-0 hover:bg-black/30"
            >
              <td className="px-4 py-3 font-mono text-xs text-[var(--eaos-text-muted)]">
                {shortId(agent.id)}
              </td>
              <td className="px-4 py-3">
                <span className="hover:underline">{agent.name}</span>
              </td>
              <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                {agent.provider}
              </td>
              <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                {agent.model ?? "—"}
              </td>
              <td className="px-4 py-3">
                <StatusBadge status={agent.status} />
              </td>
              <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                {formatDate(agent.createdAt)}
              </td>
              <td className="px-4 py-3">
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    if (confirm(`Delete agent "${agent.name}"? This removes the pod, data, and vault.`)) {
                      onDelete(agent.id);
                    }
                  }}
                  className="rounded p-1 text-[var(--eaos-text-muted)] hover:text-red-300"
                  aria-label={`Delete ${agent.name}`}
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

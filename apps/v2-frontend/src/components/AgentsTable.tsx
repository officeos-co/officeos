import Link from "next/link";
import type { Agent } from "../hooks/useAgents";
import { formatDate, shortId } from "../utils/format";

type AgentsTableProps = {
  agents: Agent[];
};

export function AgentsTable({ agents }: AgentsTableProps) {
  return (
    <div className="mx-8 my-6 overflow-hidden rounded-xl border border-[var(--eaos-border)] bg-[var(--eaos-panel)]">
      <table className="w-full text-sm">
        <thead className="text-left text-[var(--eaos-text-muted)]">
          <tr className="border-b border-[var(--eaos-border)]">
            <th className="px-4 py-3 font-normal">ID</th>
            <th className="px-4 py-3 font-normal">Name</th>
            <th className="px-4 py-3 font-normal">Model</th>
            <th className="px-4 py-3 font-normal">Status</th>
            <th className="px-4 py-3 font-normal">Created</th>
          </tr>
        </thead>
        <tbody>
          {agents.map((agent) => (
            <tr
              key={agent.id}
              className="border-b border-[var(--eaos-border)] last:border-b-0 hover:bg-black/30"
            >
              <td className="px-4 py-3 font-mono text-xs text-[var(--eaos-text-muted)]">
                {shortId(agent.id)}
              </td>
              <td className="px-4 py-3">
                <Link
                  href={`/agents/${agent.id}`}
                  className="hover:underline"
                >
                  {agent.name}
                </Link>
              </td>
              <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                {agent.model ?? "—"}
              </td>
              <td className="px-4 py-3">
                <span className="rounded-full border border-[var(--eaos-border)] px-2 py-0.5 text-xs">
                  {agent.status}
                </span>
              </td>
              <td className="px-4 py-3 text-[var(--eaos-text-muted)]">
                {formatDate(agent.createdAt)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

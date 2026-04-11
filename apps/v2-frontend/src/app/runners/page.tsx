"use client";

import { useState } from "react";
import { TopBar } from "@/components/TopBar";
import { StatusBadge } from "@/components/StatusBadge";
import { NewRunnerOverlay } from "@/components/NewRunnerOverlay";
import { useRunners } from "@/hooks/useRunners";

export default function RunnersPage() {
  const { runners, loading, error, remove } = useRunners();
  const [showNew, setShowNew] = useState(false);

  const online = runners.filter((r) => r.status === "online").length;

  return (
    <div>
      <TopBar
        title="Runners"
        subtitle={`${online} of ${runners.length} online — self-hosted skill execution in your network.`}
        action={
          <button
            onClick={() => setShowNew(true)}
            className="rounded-md bg-white px-4 py-2 text-sm font-medium text-black"
          >
            Create runner
          </button>
        }
      />

      {loading ? (
        <div className="px-8 py-12 text-sm text-[var(--eaos-text-muted)]">Loading...</div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-300">
          {error}
        </div>
      ) : runners.length === 0 ? (
        <div className="px-8 py-12 text-center">
          <p className="text-sm text-[var(--eaos-text-muted)]">
            No runners yet. Create one to execute skills inside your network.
          </p>
        </div>
      ) : (
        <div className="px-8 py-6">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-[var(--eaos-border)] text-left text-[var(--eaos-text-muted)]">
                <th className="pb-3 font-medium">Name</th>
                <th className="pb-3 font-medium">Status</th>
                <th className="pb-3 font-medium">Last heartbeat</th>
                <th className="pb-3 font-medium">Version</th>
                <th className="pb-3 font-medium">Created</th>
                <th className="pb-3 font-medium"></th>
              </tr>
            </thead>
            <tbody>
              {runners.map((runner) => (
                <tr
                  key={runner.id}
                  className="border-b border-[var(--eaos-border)] hover:bg-[var(--eaos-panel)]"
                >
                  <td className="py-3 font-medium">{runner.name}</td>
                  <td className="py-3">
                    <StatusBadge status={runner.status} />
                  </td>
                  <td className="py-3 text-[var(--eaos-text-muted)]">
                    {runner.lastHeartbeatAt
                      ? new Date(runner.lastHeartbeatAt).toLocaleString()
                      : "—"}
                  </td>
                  <td className="py-3 text-[var(--eaos-text-muted)]">
                    {runner.version ?? "—"}
                  </td>
                  <td className="py-3 text-[var(--eaos-text-muted)]">
                    {new Date(runner.createdAt).toLocaleDateString()}
                  </td>
                  <td className="py-3 text-right">
                    <button
                      onClick={() => remove(runner.id)}
                      className="text-xs text-red-400 hover:text-red-300"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <NewRunnerOverlay open={showNew} onClose={() => setShowNew(false)} />
    </div>
  );
}

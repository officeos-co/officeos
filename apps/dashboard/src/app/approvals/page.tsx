"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { useApprovalRequests, type ApprovalRequest } from "@/hooks/useApprovalRequests";
import { cn } from "@/lib/utils";

function formatRelative(iso: string) {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}h ago`;
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

function truncateId(id: string) {
  return id.length > 12 ? `${id.slice(0, 8)}…` : id;
}

function StatusBadge({ status }: { status: "approved" | "rejected" }) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide",
        status === "approved"
          ? "bg-emerald-500/15 text-emerald-500"
          : "bg-muted text-muted-foreground",
      )}
    >
      {status}
    </span>
  );
}

export default function ApprovalsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const tab = searchParams.get("tab") === "history" ? "history" : "pending";

  const { requests, loading, error, approve, reject } = useApprovalRequests();
  const [actioning, setActioning] = useState<Record<string, "approving" | "rejecting">>({});

  const pendingItems = requests.filter((r) => r.status === "pending");
  const historyItems = requests.filter((r) => r.status !== "pending");

  async function handleApprove(id: string) {
    setActioning((prev) => ({ ...prev, [id]: "approving" }));
    try {
      await approve(id);
    } finally {
      setActioning((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    }
  }

  async function handleReject(id: string) {
    setActioning((prev) => ({ ...prev, [id]: "rejecting" }));
    try {
      await reject(id);
    } finally {
      setActioning((prev) => {
        const next = { ...prev };
        delete next[id];
        return next;
      });
    }
  }

  const rows = tab === "pending" ? pendingItems : historyItems;

  return (
    <>
      <TopBar
        title="Approvals"
        subtitle="Review and action pending agent approval requests"
      />

      <div className="p-8">
        {/* Tabs */}
        <div className="mb-6 flex gap-1 rounded-lg border border-border bg-muted/40 p-1 w-fit">
          {(["pending", "history"] as const).map((t) => (
            <button
              key={t}
              type="button"
              onClick={() => router.push(`/approvals?tab=${t}`)}
              className={cn(
                "rounded-md px-4 py-1.5 text-[13px] font-medium transition-colors capitalize",
                tab === t
                  ? "bg-background text-foreground shadow-sm"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              {t}
            </button>
          ))}
        </div>

        {error && (
          <div className="mb-4 rounded-lg border border-red-500/20 bg-red-500/10 px-4 py-3 text-[13px] text-red-400">
            {error}
          </div>
        )}

        {loading ? (
          <div className="flex items-center justify-center py-20">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        ) : rows.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 text-muted-foreground">
            <p className="text-sm">
              {tab === "pending" ? "No pending approvals." : "No approval history yet."}
            </p>
          </div>
        ) : (
          <div className="rounded-lg border border-border overflow-hidden">
            <table className="w-full text-[13px]">
              <thead>
                <tr className="border-b border-border bg-muted/30">
                  <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                    Agent
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                    Skill · Action
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                    Params
                  </th>
                  <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                    Requested
                  </th>
                  {tab === "history" && (
                    <>
                      <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                        Status
                      </th>
                      <th className="px-4 py-3 text-left font-medium text-muted-foreground">
                        Decided
                      </th>
                    </>
                  )}
                  {tab === "pending" && (
                    <th className="px-4 py-3 text-right font-medium text-muted-foreground">
                      Actions
                    </th>
                  )}
                </tr>
              </thead>
              <tbody>
                {rows.map((req: ApprovalRequest) => {
                  const isActioning = !!actioning[req.id];
                  return (
                    <tr
                      key={req.id}
                      className="border-b border-border last:border-0 hover:bg-muted/20 transition-colors"
                    >
                      <td className="px-4 py-3 font-mono text-muted-foreground">
                        {truncateId(req.agentId)}
                      </td>
                      <td className="px-4 py-3 text-foreground">
                        <span className="font-medium">{req.skillName}</span>
                        <span className="text-muted-foreground"> · {req.action}</span>
                      </td>
                      <td className="px-4 py-3 font-mono text-muted-foreground max-w-[240px] truncate">
                        {req.paramsJson.slice(0, 60)}
                        {req.paramsJson.length > 60 ? "…" : ""}
                      </td>
                      <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                        {formatRelative(req.requestedAt)}
                      </td>
                      {tab === "history" && (
                        <>
                          <td className="px-4 py-3">
                            <StatusBadge status={req.status as "approved" | "rejected"} />
                          </td>
                          <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                            {req.decidedAt ? formatRelative(req.decidedAt) : "—"}
                          </td>
                        </>
                      )}
                      {tab === "pending" && (
                        <td className="px-4 py-3 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              type="button"
                              disabled={isActioning}
                              onClick={() => handleApprove(req.id)}
                              className="inline-flex items-center gap-1.5 rounded-md bg-emerald-500/15 px-3 py-1.5 text-[12px] font-medium text-emerald-500 transition-colors hover:bg-emerald-500/25 disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              {isActioning && actioning[req.id] === "approving" ? (
                                <span className="h-3 w-3 animate-spin rounded-full border border-emerald-500 border-t-transparent" />
                              ) : null}
                              Approve
                            </button>
                            <button
                              type="button"
                              disabled={isActioning}
                              onClick={() => handleReject(req.id)}
                              className="inline-flex items-center gap-1.5 rounded-md bg-muted px-3 py-1.5 text-[12px] font-medium text-muted-foreground transition-colors hover:bg-muted/80 hover:text-foreground disabled:opacity-50 disabled:cursor-not-allowed"
                            >
                              {isActioning && actioning[req.id] === "rejecting" ? (
                                <span className="h-3 w-3 animate-spin rounded-full border border-muted-foreground border-t-transparent" />
                              ) : null}
                              Reject
                            </button>
                          </div>
                        </td>
                      )}
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}

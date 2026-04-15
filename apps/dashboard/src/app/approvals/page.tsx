"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { useApprovalRequests, type ApprovalRequest } from "@/hooks/useApprovalRequests";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

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

function TableSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center gap-4 py-3">
          <Skeleton className="h-4 w-16" />
          <Skeleton className="h-4 w-40" />
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-4 w-16" />
        </div>
      ))}
    </div>
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

  return (
    <>
      <TopBar title="Approvals" />

      <div className="px-6 py-4">
        <Tabs
          value={tab}
          onValueChange={(val) => router.push(`/approvals?tab=${val}`)}
        >
          <TabsList className="h-8">
            <TabsTrigger value="pending" className="text-[12px] h-6">
              Pending {pendingItems.length > 0 && `(${pendingItems.length})`}
            </TabsTrigger>
            <TabsTrigger value="history" className="text-[12px] h-6">
              History
            </TabsTrigger>
          </TabsList>

          {error && (
            <div className="mt-4 rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
              {error}
            </div>
          )}

          <TabsContent value="pending" className="mt-4">
            {loading ? (
              <TableSkeleton />
            ) : pendingItems.length === 0 ? (
              <EmptyState
                title="No pending approvals"
                description="Agent approval requests will appear here."
              />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Agent</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Skill / Action</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Params</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Requested</TableHead>
                    <TableHead className="text-right text-[12px] font-normal text-muted-foreground">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pendingItems.map((req: ApprovalRequest) => {
                    const isActioning = !!actioning[req.id];
                    return (
                      <TableRow key={req.id}>
                        <TableCell className="font-mono text-[12px] text-muted-foreground">
                          {truncateId(req.agentId)}
                        </TableCell>
                        <TableCell className="text-[13px]">
                          <span className="font-medium">{req.skillName}</span>
                          <span className="text-muted-foreground"> / {req.action}</span>
                        </TableCell>
                        <TableCell className="font-mono text-[12px] text-muted-foreground max-w-[200px] truncate">
                          {req.paramsJson.slice(0, 50)}
                          {req.paramsJson.length > 50 ? "…" : ""}
                        </TableCell>
                        <TableCell className="text-[12px] text-muted-foreground whitespace-nowrap">
                          {formatRelative(req.requestedAt)}
                        </TableCell>
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <Button
                              size="sm"
                              disabled={isActioning}
                              onClick={() => handleApprove(req.id)}
                              className="h-7 text-[12px]"
                            >
                              Approve
                            </Button>
                            <Button
                              variant="ghost"
                              size="sm"
                              disabled={isActioning}
                              onClick={() => handleReject(req.id)}
                              className="h-7 text-[12px]"
                            >
                              Reject
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}
          </TabsContent>

          <TabsContent value="history" className="mt-4">
            {loading ? (
              <TableSkeleton />
            ) : historyItems.length === 0 ? (
              <EmptyState
                title="No approval history"
                description="Actioned requests will appear here."
              />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Agent</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Skill / Action</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Params</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Requested</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
                    <TableHead className="text-[12px] font-normal text-muted-foreground">Decided</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {historyItems.map((req: ApprovalRequest) => (
                    <TableRow key={req.id}>
                      <TableCell className="font-mono text-[12px] text-muted-foreground">
                        {truncateId(req.agentId)}
                      </TableCell>
                      <TableCell className="text-[13px]">
                        <span className="font-medium">{req.skillName}</span>
                        <span className="text-muted-foreground"> / {req.action}</span>
                      </TableCell>
                      <TableCell className="font-mono text-[12px] text-muted-foreground max-w-[200px] truncate">
                        {req.paramsJson.slice(0, 50)}
                        {req.paramsJson.length > 50 ? "…" : ""}
                      </TableCell>
                      <TableCell className="text-[12px] text-muted-foreground whitespace-nowrap">
                        {formatRelative(req.requestedAt)}
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={req.status} />
                      </TableCell>
                      <TableCell className="text-[12px] text-muted-foreground whitespace-nowrap">
                        {req.decidedAt ? formatRelative(req.decidedAt) : "—"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </TabsContent>
        </Tabs>
      </div>
    </>
  );
}

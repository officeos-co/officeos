"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
import { EmptyState } from "@/components/shared/EmptyState";
import { NewRunnerOverlay } from "@/components/runners/NewRunnerOverlay";
import { useRunners } from "@/hooks/useRunners";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Plus, Trash2 } from "lucide-react";

export default function RunnersPage() {
  const { runners, loading, error, remove } = useRunners();
  const [showNew, setShowNew] = useState(false);

  const online = runners.filter((r) => r.status === "online").length;

  return (
    <div>
      <TopBar
        title="Runners"
        subtitle={runners.length > 0 ? `${online} of ${runners.length} online` : undefined}
        action={
          <Button size="sm" onClick={() => setShowNew(true)} className="h-7">
            <Plus className="mr-1 h-3 w-3" />
            Create runner
          </Button>
        }
      />

      {error && (
        <div className="mx-6 mt-4 rounded-md border border-destructive/20 bg-destructive/5 px-4 py-2.5 text-[13px] text-destructive">
          {error}
        </div>
      )}

      {loading ? (
        <div className="px-6 py-6 space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="flex items-center gap-4 py-3">
              <Skeleton className="h-4 w-28" />
              <Skeleton className="h-3 w-16" />
              <Skeleton className="h-3 w-36" />
              <Skeleton className="h-3 w-12" />
            </div>
          ))}
        </div>
      ) : runners.length === 0 ? (
        <EmptyState
          title="No runners yet"
          description="Create a runner to execute tools inside your network."
          action={
            <Button size="sm" onClick={() => setShowNew(true)}>
              Create runner
            </Button>
          }
        />
      ) : (
        <div className="px-6 py-4">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[12px] font-normal text-muted-foreground">Name</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Status</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Last heartbeat</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Version</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Created</TableHead>
                <TableHead className="w-[40px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {runners.map((runner) => (
                <TableRow key={runner.id}>
                  <TableCell className="text-[13px] font-medium">{runner.name}</TableCell>
                  <TableCell>
                    <StatusBadge status={runner.status} />
                  </TableCell>
                  <TableCell className="text-[13px] text-muted-foreground">
                    {runner.lastHeartbeatAt
                      ? new Date(runner.lastHeartbeatAt).toLocaleString()
                      : "—"}
                  </TableCell>
                  <TableCell className="text-[13px] text-muted-foreground">
                    {runner.version ?? "—"}
                  </TableCell>
                  <TableCell className="text-[13px] text-muted-foreground">
                    {new Date(runner.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => remove(runner.id)}
                      className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      <NewRunnerOverlay open={showNew} onClose={() => setShowNew(false)} />
    </div>
  );
}

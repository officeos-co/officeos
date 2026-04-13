"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { StatusBadge } from "@/components/shared/StatusBadge";
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
        subtitle={`${online} of ${runners.length} online`}
        action={
          <Button size="sm" onClick={() => setShowNew(true)}>
            <Plus className="mr-1.5 h-3.5 w-3.5" />
            Create runner
          </Button>
        }
      />

      {loading ? (
        <div className="px-8 py-6">
          <div className="rounded-lg border border-border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Last heartbeat</TableHead>
                  <TableHead>Version</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
                    <TableCell><Skeleton className="h-4 w-28" /></TableCell>
                    <TableCell><Skeleton className="h-5 w-16 rounded-full" /></TableCell>
                    <TableCell><Skeleton className="h-3 w-36" /></TableCell>
                    <TableCell><Skeleton className="h-3 w-12" /></TableCell>
                    <TableCell><Skeleton className="h-3 w-24" /></TableCell>
                    <TableCell><Skeleton className="h-7 w-7 rounded-md" /></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      ) : error ? (
        <div className="mx-8 mt-6 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      ) : runners.length === 0 ? (
        <div className="px-8 py-16 text-center">
          <p className="text-sm text-muted-foreground">
            No runners yet. Create one to execute tools inside your network.
          </p>
        </div>
      ) : (
        <div className="px-8 py-6">
          <div className="rounded-lg border border-border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Last heartbeat</TableHead>
                  <TableHead>Version</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {runners.map((runner) => (
                  <TableRow key={runner.id}>
                    <TableCell className="font-medium">{runner.name}</TableCell>
                    <TableCell>
                      <StatusBadge status={runner.status} />
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {runner.lastHeartbeatAt
                        ? new Date(runner.lastHeartbeatAt).toLocaleString()
                        : "\u2014"}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {runner.version ?? "\u2014"}
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(runner.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => remove(runner.id)}
                        className="h-7 w-7 p-0 text-destructive hover:text-destructive"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      )}

      <NewRunnerOverlay open={showNew} onClose={() => setShowNew(false)} />
    </div>
  );
}

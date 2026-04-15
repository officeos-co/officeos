"use client";

import { useState } from "react";
import { TopBar } from "@/components/shared/TopBar";
import { EmptyState } from "@/components/shared/EmptyState";
import { useSystemEvents } from "@/hooks/useSystemEvents";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { Check } from "lucide-react";

type SeverityFilter = "all" | "error" | "warning" | "info";

const severityColor: Record<string, string> = {
  error: "text-red-600",
  warning: "text-amber-600",
  info: "text-muted-foreground",
};

function formatTime(iso: string) {
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

export default function SystemEventsPage() {
  const { events, loading, unacknowledgedCount, acknowledge } = useSystemEvents(100);
  const [severityFilter, setSeverityFilter] = useState<SeverityFilter>("all");

  const filteredEvents =
    severityFilter === "all" ? events : events.filter((e) => e.severity === severityFilter);

  return (
    <>
      <TopBar
        title="Events"
        action={
          <div className="flex items-center gap-1">
            {(["all", "error", "warning", "info"] as const).map((f) => (
              <Button
                key={f}
                variant="ghost"
                size="sm"
                onClick={() => setSeverityFilter(f)}
                className={cn(
                  "h-7 text-[12px] capitalize",
                  severityFilter === f && "bg-accent text-accent-foreground",
                )}
              >
                {f === "all" ? "All" : f.charAt(0).toUpperCase() + f.slice(1) + "s"}
              </Button>
            ))}
            {unacknowledgedCount > 0 && (
              <span className="ml-1 text-[11px] text-red-600 font-medium">
                {unacknowledgedCount} unack
              </span>
            )}
          </div>
        }
      />

      <div className="px-6 py-4">
        {loading ? (
          <div className="space-y-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4 py-3">
                <Skeleton className="h-3 w-12" />
                <Skeleton className="h-4 w-64" />
                <Skeleton className="h-3 w-16" />
              </div>
            ))}
          </div>
        ) : filteredEvents.length === 0 ? (
          <EmptyState
            title={severityFilter === "all" ? "No system events" : `No ${severityFilter} events`}
            description="Events will appear here in real time."
            action={
              severityFilter !== "all" ? (
                <Button variant="ghost" size="sm" onClick={() => setSeverityFilter("all")}>
                  Show all
                </Button>
              ) : undefined
            }
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="text-[12px] font-normal text-muted-foreground w-[80px]">Severity</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Message</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Category</TableHead>
                <TableHead className="text-[12px] font-normal text-muted-foreground">Time</TableHead>
                <TableHead className="w-[40px]" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredEvents.map((ev) => (
                <TableRow
                  key={ev.id}
                  className={cn(ev.acknowledged && "opacity-50")}
                >
                  <TableCell>
                    <span className={cn("text-[12px] font-medium capitalize", severityColor[ev.severity] ?? "text-muted-foreground")}>
                      {ev.severity}
                    </span>
                  </TableCell>
                  <TableCell className="text-[13px]">
                    {ev.message}
                    {ev.skillName && (
                      <span className="ml-2 text-[11px] text-muted-foreground">{ev.skillName}</span>
                    )}
                  </TableCell>
                  <TableCell className="text-[12px] font-mono text-muted-foreground">
                    {ev.category}
                  </TableCell>
                  <TableCell className="text-[12px] text-muted-foreground whitespace-nowrap">
                    {formatTime(ev.createdAt)}
                  </TableCell>
                  <TableCell>
                    {!ev.acknowledged && (
                      <button
                        type="button"
                        onClick={() => acknowledge(ev.id)}
                        className="rounded p-1 text-muted-foreground hover:text-foreground hover:bg-accent"
                        title="Acknowledge"
                      >
                        <Check className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </div>
    </>
  );
}

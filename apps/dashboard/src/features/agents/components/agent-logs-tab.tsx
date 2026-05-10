"use client";

import type { ReactNode } from "react";
import { LogDetailPanel } from "@/features/analytics";
import { LogTable } from "@/features/analytics";
import { SearchInput } from "@/ui/search-input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { Switch } from "@/ui/switch";
import { Label } from "@/ui/label";
import { useAutoScrollToBottom } from "../hooks/useAutoScrollToBottom";
import {
  AGENT_LOG_TYPES,
  useAgentLogTimeline,
} from "../hooks/useAgentLogTimeline";
import { BugIcon } from "lucide-react";

export function AgentLogsTab({
  agentId,
  composer,
  pendingTurnStartedAt = null,
}: {
  agentId: string;
  composer?: ReactNode;
  pendingTurnStartedAt?: number | null;
}) {
  const timeline = useAgentLogTimeline({ agentId, pendingTurnStartedAt });
  const scrollRef = useAutoScrollToBottom<HTMLDivElement>({
    rowCount: timeline.visibleLogs.length + (timeline.showThinkingRow ? 1 : 0),
    resetKey: `${timeline.search}:${timeline.typeFilter}:${timeline.debugLogs}`,
  });

  return (
    <div
      className={
        timeline.selectedLog
          ? "grid h-full min-h-0 flex-1 overflow-hidden grid-cols-[minmax(0,1fr)_clamp(360px,42vw,560px)]"
          : "flex h-full min-h-0 flex-1 flex-col overflow-hidden"
      }
    >
      <div className="flex h-full min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <section className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden bg-background pt-4">
          <div className="flex min-h-14 shrink-0 flex-wrap items-center justify-between gap-3 py-2">
            <div className="flex items-center gap-2">
              <SearchInput
                placeholder="Search logs..."
                value={timeline.search}
                onChange={timeline.setSearch}
              />
              <Select
                value={timeline.typeFilter}
                onValueChange={(value) => {
                  if (value) timeline.setTypeFilter(value);
                }}
              >
                <SelectTrigger className="w-[150px]">
                  <SelectValue placeholder="Type" />
                </SelectTrigger>
                <SelectContent>
                  {AGENT_LOG_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type === "All" ? "All types" : type.replace("_", " ")}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <Label className="shrink-0 gap-2 rounded-md border border-border px-2.5 py-2 text-xs text-muted-foreground">
              <BugIcon className="size-3.5" />
              Debug
              <Switch
                size="sm"
                checked={timeline.debugLogs}
                onCheckedChange={timeline.setDebugLogs}
              />
            </Label>
          </div>

          <div
            ref={scrollRef}
            className="min-h-0 flex-1 overflow-y-scroll pr-4 [scrollbar-gutter:stable]"
          >
            <LogTable
              logs={timeline.visibleLogs}
              selectedLogId={timeline.selectedLogId}
              showSelectionColumn={false}
              loading={timeline.loading && timeline.logs.length === 0}
              skeletonRows={10}
              thinking={timeline.showThinkingRow}
              className="[&_tr]:border-0"
              onSelectLog={timeline.selectLog}
            />
          </div>
        </section>

        {composer && (
          <div className="shrink-0 border-t border-border bg-background/80 p-3 backdrop-blur-sm">
            {composer}
          </div>
        )}
      </div>

      {timeline.selectedLog && (
        <div className="h-full min-h-0 overflow-hidden border-l border-border">
          <LogDetailPanel
            log={timeline.selectedLog}
            onClose={timeline.closeDetail}
            className="w-full border-l-0"
          />
        </div>
      )}
    </div>
  );
}

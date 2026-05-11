"use client";

import { useState } from "react";
import {
  CalendarClockIcon,
  GitBranchIcon,
  PlusIcon,
  Trash2Icon,
} from "lucide-react";
import { Button } from "@/ui/button";
import { EmptyState } from "@/ui/empty-state";
import { Skeleton } from "@/ui/skeleton";
import { Switch } from "@/ui/switch";
import { WithTooltip } from "@/ui/help-tooltip";
import {
  latestRoutineRunAt,
  nextRoutineRunAt,
  parseScheduleExpression,
  useAgentRoutines,
} from "../api/useRoutines";
import {
  describeTrigger,
  formatDateTime,
  routineTriggerSummary,
  triggerKindLabel,
} from "./routine-display";
import { RoutineCreateDialog } from "./routine-create-dialog";

export function AgentRoutinesTab({
  agentId,
  agentName,
}: {
  agentId: string;
  agentName?: string;
}) {
  const {
    routines,
    loading,
    creating,
    createRoutine,
    setRoutineEnabled,
    deleteRoutine,
    refetch,
  } = useAgentRoutines(agentId);
  const [dialogOpen, setDialogOpen] = useState(false);

  if (loading && routines.length === 0) {
    return (
      <div className="pt-4">
        <Skeleton className="h-32 w-full rounded-xl" />
      </div>
    );
  }

  return (
    <div className="space-y-4 pt-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium">Routines</h3>
          <p className="text-xs text-muted-foreground">
            Run this agent from schedules, API calls, or GitHub webhooks.
          </p>
        </div>
        <WithTooltip tooltip="Create an agent routine with one or more triggers.">
          <Button size="sm" onClick={() => setDialogOpen(true)}>
            <PlusIcon className="size-3.5" />
            New routine
          </Button>
        </WithTooltip>
      </div>

      <div className="space-y-2">
        {routines.map((routine) => (
          <div key={routine.id} className="rounded-lg border border-border">
            <div className="flex items-center gap-4 px-4 py-3">
              <Switch
                checked={routine.enabled}
                onCheckedChange={() =>
                  setRoutineEnabled(routine.id, !routine.enabled)
                }
              />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <GitBranchIcon className="size-3.5 text-muted-foreground" />
                  <span
                    className={`text-sm font-medium ${
                      !routine.enabled ? "text-muted-foreground" : ""
                    }`}
                  >
                    {routine.name}
                  </span>
                </div>
                <div className="mt-0.5 text-xs text-muted-foreground">
                  {routineTriggerSummary(routine)}
                </div>
              </div>
              <div className="hidden shrink-0 text-right text-xs text-muted-foreground sm:block">
                <div>Last: {formatDateTime(latestRoutineRunAt(routine))}</div>
                <div>
                  Next:{" "}
                  {nextRoutineRunAt(routine)
                    ? formatDateTime(nextRoutineRunAt(routine))
                    : "Pending"}
                </div>
              </div>
              <WithTooltip tooltip="Delete this routine. This does not delete agent logs or memory.">
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="text-muted-foreground hover:text-destructive"
                  onClick={() => deleteRoutine(routine.id)}
                >
                  <Trash2Icon className="size-4" />
                </Button>
              </WithTooltip>
            </div>
            <div className="grid gap-3 border-t border-border px-4 py-3 text-sm sm:grid-cols-2">
              {routine.triggers.map((trigger) => (
                <div key={trigger.id}>
                  <div className="text-xs text-muted-foreground">
                    {triggerKindLabel(trigger.kind)}: {trigger.name}
                  </div>
                  <div className="mt-0.5 flex items-center gap-1.5">
                    {trigger.kind === "schedule" && (
                      <CalendarClockIcon className="size-3.5 text-muted-foreground" />
                    )}
                    <span>{describeTrigger(trigger)}</span>
                  </div>
                  {trigger.kind === "schedule" && (
                    <code className="mt-1 block font-mono text-xs text-muted-foreground">
                      {parseScheduleExpression(trigger)}
                    </code>
                  )}
                </div>
              ))}
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="mb-1 text-xs text-muted-foreground">Prompt</div>
              <p className="whitespace-pre-wrap text-sm">{routine.prompt}</p>
            </div>
          </div>
        ))}
      </div>

      {routines.length === 0 && (
        <EmptyState message="No routines found." />
      )}

      <RoutineCreateDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        fixedAgentId={agentId}
        fixedAgentName={agentName}
        creating={creating}
        createRoutine={createRoutine}
        onCreated={() => refetch()}
      />
    </div>
  );
}

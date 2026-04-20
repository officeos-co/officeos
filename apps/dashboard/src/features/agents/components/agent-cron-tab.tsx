"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useCronJobs } from "@/features/agents/api/useCronJobs";
import { ClockIcon, PlusIcon, Trash2Icon } from "lucide-react";

const CRON_PRESETS = [
  { label: "Every hour", expression: "0 * * * *" },
  { label: "Every day at 9 AM", expression: "0 9 * * *" },
  { label: "Every weekday at 9 AM", expression: "0 9 * * 1-5" },
  { label: "Every Monday at 9 AM", expression: "0 9 * * 1" },
  { label: "Every Friday at 5 PM", expression: "0 17 * * 5" },
  { label: "1st of every month", expression: "0 8 1 * *" },
];

export function AgentCronTab({ agentId }: { agentId: string }) {
  const { jobs, loading, createCronJob, setCronJobEnabled, deleteCronJob } =
    useCronJobs(agentId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState("");
  const [expression, setExpression] = useState("");
  const [prompt, setPrompt] = useState("");

  async function handleCreate() {
    await createCronJob(name || "Untitled job", expression, prompt);
    setDialogOpen(false);
    setName("");
    setExpression("");
    setPrompt("");
  }

  if (loading)
    return (
      <div className="pt-4">
        <Skeleton className="h-32 w-full rounded-xl" />
      </div>
    );

  return (
    <div className="pt-4 space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium">Scheduled tasks</h3>
          <p className="text-xs text-muted-foreground">
            Cron jobs run this agent on a schedule with a specific prompt.
          </p>
        </div>
        <Button size="sm" onClick={() => setDialogOpen(true)}>
          <PlusIcon className="size-3.5" />
          New schedule
        </Button>
      </div>

      {/* Job list */}
      <div className="space-y-2">
        {jobs.map((job) => (
          <div key={job.id} className="rounded-xl border border-border">
            <div className="flex items-center gap-4 px-4 py-3">
              <Switch
                checked={job.enabled}
                onCheckedChange={() => setCronJobEnabled(job.id, !job.enabled)}
              />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <span
                    className={`text-sm font-medium ${!job.enabled ? "text-muted-foreground" : ""}`}
                  >
                    {job.name}
                  </span>
                  <code className="rounded bg-muted px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">
                    {job.expression}
                  </code>
                </div>
                <div className="text-xs text-muted-foreground mt-0.5">
                  {CRON_PRESETS.find((p) => p.expression === job.expression)
                    ?.label ?? job.expression}
                </div>
              </div>
              <div className="flex items-center gap-4 shrink-0 text-xs text-muted-foreground">
                <div className="text-right hidden sm:block">
                  <div>
                    Last:{" "}
                    {job.lastRunAt
                      ? new Date(job.lastRunAt).toLocaleString()
                      : "Never"}
                  </div>
                  <div>
                    Next:{" "}
                    {job.nextRunAt
                      ? new Date(job.nextRunAt).toLocaleString()
                      : "Pending"}
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                  onClick={() => deleteCronJob(job.id)}
                >
                  <Trash2Icon className="size-3.5" />
                </Button>
              </div>
            </div>
            <div className="border-t border-border px-4 py-3">
              <div className="text-xs text-muted-foreground mb-1">Prompt</div>
              <p className="text-sm">{job.prompt}</p>
            </div>
          </div>
        ))}
      </div>

      {jobs.length === 0 && (
        <div className="flex flex-col items-center justify-center py-12 text-center">
          <ClockIcon className="size-8 text-muted-foreground/30 mb-3" />
          <p className="text-sm font-medium">No scheduled tasks</p>
          <p className="text-sm text-muted-foreground mt-1">
            Create a cron job to run this agent on a schedule.
          </p>
          <Button
            size="sm"
            className="mt-4"
            onClick={() => setDialogOpen(true)}
          >
            <PlusIcon className="size-3.5" /> New schedule
          </Button>
        </div>
      )}

      {/* Create dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>New scheduled task</DialogTitle>
            <DialogDescription>
              Run this agent automatically on a cron schedule.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            <div className="space-y-2">
              <Label htmlFor="cron-name">Name</Label>
              <Input
                id="cron-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Daily research digest"
              />
            </div>

            <div className="space-y-2">
              <Label>Schedule</Label>
              <div className="flex flex-wrap gap-1.5">
                {CRON_PRESETS.map((p) => (
                  <button
                    key={p.expression}
                    type="button"
                    onClick={() => setExpression(p.expression)}
                    className={`rounded-lg border px-3 py-1.5 text-xs transition-colors ${
                      expression === p.expression
                        ? "border-primary bg-primary/5"
                        : "border-border hover:bg-muted/50"
                    }`}
                  >
                    {p.label}
                  </button>
                ))}
              </div>
              <div className="flex items-center gap-2">
                <Input
                  value={expression}
                  onChange={(e) => setExpression(e.target.value)}
                  placeholder="Custom: 0 9 * * 1-5"
                  className="max-w-[200px] font-mono text-xs"
                />
                {expression && (
                  <span className="text-xs text-muted-foreground">
                    {CRON_PRESETS.find((p) => p.expression === expression)
                      ?.label ?? "Custom schedule"}
                  </span>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="cron-prompt">Prompt</Label>
              <Textarea
                id="cron-prompt"
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder="What should the agent do on this schedule?"
                rows={3}
              />
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setDialogOpen(false)}
            >
              Cancel
            </Button>
            <Button
              size="sm"
              onClick={handleCreate}
              disabled={!expression.trim()}
            >
              Create schedule
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

"use client";

import { useState } from "react";
import { Button } from "@/ui/button";
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip";
import { Input } from "@/ui/input";
import { Textarea } from "@/ui/textarea";
import { Label } from "@/ui/label";
import { Switch } from "@/ui/switch";
import { Skeleton } from "@/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { useCronJobs } from "../api/useCronJobs";
import {
  ClockIcon,
  PlusIcon,
  Trash2Icon,
  HeartPulseIcon,
} from "lucide-react";

type Frequency =
  | "every-30-min"
  | "every-hour"
  | "every-day"
  | "every-weekday"
  | "every-week"
  | "every-month";

const FREQUENCIES: { value: Frequency; label: string; description: string }[] =
  [
    {
      value: "every-30-min",
      label: "Every 30 minutes",
      description: "Heartbeat — keeps the agent checking in regularly",
    },
    {
      value: "every-hour",
      label: "Every hour",
      description: "Runs once per hour at the top of the hour",
    },
    {
      value: "every-day",
      label: "Every day",
      description: "Runs once a day at a specific time",
    },
    {
      value: "every-weekday",
      label: "Every weekday",
      description: "Monday through Friday at a specific time",
    },
    {
      value: "every-week",
      label: "Every week",
      description: "Once a week on a specific day and time",
    },
    {
      value: "every-month",
      label: "Every month",
      description: "Once a month on a specific day and time",
    },
  ];

const DAYS_OF_WEEK = [
  { value: "1", label: "Monday" },
  { value: "2", label: "Tuesday" },
  { value: "3", label: "Wednesday" },
  { value: "4", label: "Thursday" },
  { value: "5", label: "Friday" },
  { value: "6", label: "Saturday" },
  { value: "0", label: "Sunday" },
];

function buildCronExpression(
  frequency: Frequency,
  hour: string,
  minute: string,
  dayOfWeek: string,
  dayOfMonth: string,
): string {
  const h = parseInt(hour, 10) || 9;
  const m = parseInt(minute, 10) || 0;
  switch (frequency) {
    case "every-30-min":
      return "*/30 * * * *";
    case "every-hour":
      return "0 * * * *";
    case "every-day":
      return `${m} ${h} * * *`;
    case "every-weekday":
      return `${m} ${h} * * 1-5`;
    case "every-week":
      return `${m} ${h} * * ${dayOfWeek}`;
    case "every-month":
      return `${m} ${h} ${parseInt(dayOfMonth, 10) || 1} * *`;
  }
}

function describeExpression(expression: string): string {
  const known: Record<string, string> = {
    "*/30 * * * *": "Every 30 minutes",
    "0 * * * *": "Every hour",
  };
  if (known[expression]) return known[expression];

  const parts = expression.split(" ");
  if (parts.length !== 5) return expression;
  const [min, hour, dom, , dow] = parts;

  const timeStr = `${hour.padStart(2, "0")}:${min.padStart(2, "0")}`;

  if (dom !== "*" && dow === "*") {
    return `Monthly on day ${dom} at ${timeStr}`;
  }
  if (dow === "1-5") return `Weekdays at ${timeStr}`;
  if (dow !== "*") {
    const dayName = DAYS_OF_WEEK.find((d) => d.value === dow)?.label ?? dow;
    return `Every ${dayName} at ${timeStr}`;
  }
  if (hour !== "*" && dom === "*" && dow === "*") return `Daily at ${timeStr}`;

  return expression;
}

function isHeartbeat(expression: string): boolean {
  return expression === "*/30 * * * *";
}

export function AgentCronTab({ agentId }: { agentId: string }) {
  const { jobs, loading, createCronJob, setCronJobEnabled, deleteCronJob } =
    useCronJobs(agentId);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [name, setName] = useState("");
  const [frequency, setFrequency] = useState<Frequency>("every-day");
  const [hour, setHour] = useState("09");
  const [minute, setMinute] = useState("00");
  const [dayOfWeek, setDayOfWeek] = useState("1");
  const [dayOfMonth, setDayOfMonth] = useState("1");
  const [prompt, setPrompt] = useState("");

  const needsTime = !["every-30-min", "every-hour"].includes(frequency);
  const needsDayOfWeek = frequency === "every-week";
  const needsDayOfMonth = frequency === "every-month";

  function resetForm() {
    setName("");
    setFrequency("every-day");
    setHour("09");
    setMinute("00");
    setDayOfWeek("1");
    setDayOfMonth("1");
    setPrompt("");
  }

  async function handleCreate() {
    const expression = buildCronExpression(
      frequency,
      hour,
      minute,
      dayOfWeek,
      dayOfMonth,
    );
    const jobName =
      name || (isHeartbeat(expression) ? "Heartbeat" : "Untitled schedule");
    await createCronJob(jobName, expression, prompt);
    setDialogOpen(false);
    resetForm();
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
            Run this agent on a schedule with a specific prompt.
          </p>
        </div>
        <WithTooltip tooltip="Create a backend cron job that sends this agent a prompt on schedule.">
          <Button size="sm" onClick={() => setDialogOpen(true)}>
            <PlusIcon className="size-3.5" />
            New schedule
          </Button>
        </WithTooltip>
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
                  {isHeartbeat(job.expression.value) && (
                    <HeartPulseIcon className="size-3.5 text-rose-500" />
                  )}
                  <span
                    className={`text-sm font-medium ${!job.enabled ? "text-muted-foreground" : ""}`}
                  >
                    {job.name}
                  </span>
                </div>
                <div className="text-xs text-muted-foreground mt-0.5">
                  {describeExpression(job.expression.value)}
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
                <WithTooltip tooltip="Delete this scheduled task. This does not delete agent logs or memory.">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 w-7 p-0 text-muted-foreground hover:text-destructive"
                    onClick={() => deleteCronJob(job.id)}
                  >
                    <Trash2Icon className="size-3.5" />
                  </Button>
                </WithTooltip>
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
            Create a schedule to run this agent automatically — including
            heartbeats.
          </p>
          <WithTooltip tooltip="Schedules run through the backend, so they continue even when the dashboard is closed.">
            <Button
              size="sm"
              className="mt-4"
              onClick={() => setDialogOpen(true)}
            >
              <PlusIcon className="size-3.5" /> New schedule
            </Button>
          </WithTooltip>
        </div>
      )}

      {/* Create dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>New scheduled task</DialogTitle>
            <DialogDescription>
              Run this agent automatically on a schedule. Use &ldquo;Every 30
              minutes&rdquo; for a heartbeat.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 pt-2">
            <div className="space-y-2">
              <Label htmlFor="cron-name">
                Name
                <HelpTooltip>
                  Names are only for operators. They do not change the prompt
                  sent to the agent.
                </HelpTooltip>
              </Label>
              <Input
                id="cron-name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={
                  frequency === "every-30-min"
                    ? "e.g. Heartbeat"
                    : "e.g. Daily research digest"
                }
              />
            </div>

            <div className="space-y-3">
              <Label>
                Frequency
                <HelpTooltip>
                  Frequencies are converted to cron expressions and evaluated
                  by the backend scheduler.
                </HelpTooltip>
              </Label>
              <div className="grid grid-cols-2 gap-2">
                {FREQUENCIES.map((f) => (
                  <button
                    key={f.value}
                    type="button"
                    onClick={() => setFrequency(f.value)}
                    className={`rounded-lg border px-3 py-2.5 text-left transition-colors ${
                      frequency === f.value
                        ? "border-primary bg-primary/5"
                        : "border-border hover:bg-muted/50"
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      {f.value === "every-30-min" && (
                        <HeartPulseIcon className="size-3.5 text-rose-500 shrink-0" />
                      )}
                      <span className="text-sm font-medium">{f.label}</span>
                    </div>
                    <p className="text-[11px] text-muted-foreground mt-0.5">
                      {f.description}
                    </p>
                  </button>
                ))}
              </div>

              {needsTime && (
                <div className="flex items-center gap-2">
                  <Label className="text-xs text-muted-foreground shrink-0">
                    At
                  </Label>
                  <Select value={hour} onValueChange={(v) => v && setHour(v)}>
                    <SelectTrigger className="w-[80px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {Array.from({ length: 24 }, (_, i) => (
                        <SelectItem
                          key={i}
                          value={i.toString().padStart(2, "0")}
                        >
                          {i.toString().padStart(2, "0")}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <span className="text-muted-foreground">:</span>
                  <Select value={minute} onValueChange={(v) => v && setMinute(v)}>
                    <SelectTrigger className="w-[80px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {["00", "15", "30", "45"].map((m) => (
                        <SelectItem key={m} value={m}>
                          {m}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <span className="text-xs text-muted-foreground">UTC</span>
                </div>
              )}

              {needsDayOfWeek && (
                <div className="flex items-center gap-2">
                  <Label className="text-xs text-muted-foreground shrink-0">
                    On
                  </Label>
                  <Select value={dayOfWeek} onValueChange={(v) => v && setDayOfWeek(v)}>
                    <SelectTrigger className="w-[140px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {DAYS_OF_WEEK.map((d) => (
                        <SelectItem key={d.value} value={d.value}>
                          {d.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}

              {needsDayOfMonth && (
                <div className="flex items-center gap-2">
                  <Label className="text-xs text-muted-foreground shrink-0">
                    On day
                  </Label>
                  <Select value={dayOfMonth} onValueChange={(v) => v && setDayOfMonth(v)}>
                    <SelectTrigger className="w-[80px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {Array.from({ length: 28 }, (_, i) => (
                        <SelectItem key={i + 1} value={(i + 1).toString()}>
                          {i + 1}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="cron-prompt">
                Prompt
                <HelpTooltip>
                  This exact prompt is sent to the agent whenever the schedule
                  runs.
                </HelpTooltip>
              </Label>
              <Textarea
                id="cron-prompt"
                value={prompt}
                onChange={(e) => setPrompt(e.target.value)}
                placeholder={
                  frequency === "every-30-min"
                    ? "e.g. Check for new tasks, review pending items, and report anything urgent."
                    : "What should the agent do on this schedule?"
                }
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
            <WithTooltip tooltip="Create the schedule. It can be disabled or deleted later.">
              <Button size="sm" onClick={handleCreate} disabled={!prompt.trim()}>
                Create schedule
              </Button>
            </WithTooltip>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}

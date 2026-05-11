"use client";

import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  CalendarClockIcon,
  Code2Icon,
  GitBranchIcon,
  KeyRoundIcon,
  PlusIcon,
  Trash2Icon,
} from "lucide-react";
import { getDialogWidthClassName } from "@/shell/page-container";
import { Button } from "@/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/ui/dialog";
import { HelpTooltip, WithTooltip } from "@/ui/help-tooltip";
import { Input } from "@/ui/input";
import { Label } from "@/ui/label";
import { Separator } from "@/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/ui/select";
import { Switch } from "@/ui/switch";
import { Textarea } from "@/ui/textarea";
import type {
  CreateRoutineInput,
  CreateRoutineResult,
  RoutineGeneratedSecret,
} from "../api/useRoutines";

type AgentOption = {
  id: string;
  name: string;
};

type Frequency =
  | "every-30-min"
  | "every-hour"
  | "every-day"
  | "every-weekday"
  | "every-week"
  | "every-month";

type ScheduleDraft = {
  id: string;
  type: "schedule";
  name: string;
  frequency: Frequency;
  hour: string;
  minute: string;
  dayOfWeek: string;
  dayOfMonth: string;
  advanced: boolean;
  expression: string;
};

type ApiDraft = {
  id: string;
  type: "api";
  name: string;
};

type GitHubDraft = {
  id: string;
  type: "github";
  name: string;
  owner: string;
  repo: string;
  eventsText: string;
  secret: string;
};

type TriggerDraft = ScheduleDraft | ApiDraft | GitHubDraft;

const FREQUENCIES: { value: Frequency; label: string; description: string }[] =
  [
    {
      value: "every-30-min",
      label: "Every 30 minutes",
      description: "Frequent heartbeat or monitoring loop",
    },
    {
      value: "every-hour",
      label: "Every hour",
      description: "Runs at the top of each hour",
    },
    {
      value: "every-day",
      label: "Every day",
      description: "Runs daily at a fixed UTC time",
    },
    {
      value: "every-weekday",
      label: "Every weekday",
      description: "Runs Monday through Friday",
    },
    {
      value: "every-week",
      label: "Every week",
      description: "Runs once on a selected weekday",
    },
    {
      value: "every-month",
      label: "Every month",
      description: "Runs once on a selected day",
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

const TRIGGER_OPTIONS: Array<{
  type: TriggerDraft["type"];
  label: string;
  description: string;
  icon: ReactNode;
}> = [
  {
    type: "api",
    label: "API",
    description: "Generate a secret and invoke the routine from an HTTP call.",
    icon: <Code2Icon className="size-4" />,
  },
  {
    type: "schedule",
    label: "Schedule",
    description: "Run the routine on a cron-backed recurring schedule.",
    icon: <CalendarClockIcon className="size-4" />,
  },
  {
    type: "github",
    label: "GitHub",
    description: "Run the routine from repository webhook events.",
    icon: <GitBranchIcon className="size-4" />,
  },
];

function buildCronExpression(trigger: ScheduleDraft): string {
  if (trigger.advanced) return trigger.expression.trim();

  const hour = parseInt(trigger.hour, 10) || 9;
  const minute = parseInt(trigger.minute, 10) || 0;
  if (trigger.frequency === "every-30-min") return "*/30 * * * *";
  if (trigger.frequency === "every-hour") return "0 * * * *";
  if (trigger.frequency === "every-day") return `${minute} ${hour} * * *`;
  if (trigger.frequency === "every-weekday") return `${minute} ${hour} * * 1-5`;
  if (trigger.frequency === "every-week") {
    return `${minute} ${hour} * * ${trigger.dayOfWeek}`;
  }
  return `${minute} ${hour} ${parseInt(trigger.dayOfMonth, 10) || 1} * *`;
}

function defaultTrigger(type: TriggerDraft["type"]): TriggerDraft {
  const suffix = Date.now().toString(36);
  if (type === "schedule") {
    return {
      id: crypto.randomUUID(),
      type,
      name: "Schedule",
      frequency: "every-day",
      hour: "09",
      minute: "00",
      dayOfWeek: "1",
      dayOfMonth: "1",
      advanced: false,
      expression: "0 9 * * *",
    };
  }
  if (type === "github") {
    return {
      id: crypto.randomUUID(),
      type,
      name: "GitHub webhook",
      owner: "",
      repo: "",
      eventsText: "push",
      secret: crypto.randomUUID(),
    };
  }
  return {
    id: crypto.randomUUID(),
    type,
    name: `API trigger ${suffix}`,
  };
}

function parseEvents(value: string): string[] {
  return value
    .split(/[\n,]/)
    .map((event) => event.trim())
    .filter(Boolean);
}

function isValidTrigger(trigger: TriggerDraft): boolean {
  if (!trigger.name.trim()) return false;
  if (trigger.type === "api") return true;
  if (trigger.type === "github") {
    return (
      Boolean(trigger.owner.trim()) &&
      Boolean(trigger.repo.trim()) &&
      Boolean(trigger.secret.trim()) &&
      parseEvents(trigger.eventsText).length > 0
    );
  }
  const expression = buildCronExpression(trigger);
  return expression.split(/\s+/).length === 5;
}

export function RoutineCreateDialog({
  open,
  onOpenChange,
  agents,
  fixedAgentId,
  fixedAgentName,
  creating,
  createRoutine,
  onCreated,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  agents?: AgentOption[];
  fixedAgentId?: string;
  fixedAgentName?: string;
  creating?: boolean;
  createRoutine: (input: CreateRoutineInput) => Promise<CreateRoutineResult | null>;
  onCreated?: (result: CreateRoutineResult) => void;
}) {
  const [agentId, setAgentId] = useState(fixedAgentId ?? "");
  const [name, setName] = useState("");
  const [prompt, setPrompt] = useState("");
  const [triggers, setTriggers] = useState<TriggerDraft[]>([]);
  const [generatedSecrets, setGeneratedSecrets] = useState<RoutineGeneratedSecret[]>([]);
  const [createdRoutineName, setCreatedRoutineName] = useState("");
  const shouldScrollToTriggerRef = useRef(false);
  const triggerEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!shouldScrollToTriggerRef.current || !triggerEndRef.current) return;
    shouldScrollToTriggerRef.current = false;
    triggerEndRef.current.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [triggers]);

  const selectedAgent = useMemo(
    () => agents?.find((agent) => agent.id === agentId),
    [agentId, agents],
  );
  const canCreate =
    Boolean((fixedAgentId ?? agentId).trim()) &&
    Boolean(name.trim()) &&
    Boolean(prompt.trim()) &&
    triggers.length > 0 &&
    triggers.every(isValidTrigger);

  function resetForm() {
    setAgentId(fixedAgentId ?? "");
    setName("");
    setPrompt("");
    setTriggers([]);
    setGeneratedSecrets([]);
    setCreatedRoutineName("");
  }

  function setOpen(next: boolean) {
    if (creating) return;
    if (!next) resetForm();
    onOpenChange(next);
  }

  function addTrigger(type: TriggerDraft["type"]) {
    shouldScrollToTriggerRef.current = true;
    setTriggers((prev) => [...prev, defaultTrigger(type)]);
  }

  function removeTrigger(id: string) {
    setTriggers((prev) => prev.filter((trigger) => trigger.id !== id));
  }

  function updateTrigger(id: string, patch: Partial<TriggerDraft>) {
    setTriggers((prev) =>
      prev.map((trigger) =>
        trigger.id === id ? ({ ...trigger, ...patch } as TriggerDraft) : trigger,
      ),
    );
  }

  async function submit() {
    if (!canCreate || creating) return;
    const input: CreateRoutineInput = {
      agentId: fixedAgentId ?? agentId,
      name: name.trim(),
      prompt: prompt.trim(),
      scheduleTriggers: triggers
        .filter((trigger): trigger is ScheduleDraft => trigger.type === "schedule")
        .map((trigger) => ({
          name: trigger.name.trim(),
          expression: buildCronExpression(trigger),
        })),
      apiTriggers: triggers
        .filter((trigger): trigger is ApiDraft => trigger.type === "api")
        .map((trigger) => ({ name: trigger.name.trim() })),
      gitHubTriggers: triggers
        .filter((trigger): trigger is GitHubDraft => trigger.type === "github")
        .map((trigger) => ({
          name: trigger.name.trim(),
          owner: trigger.owner.trim(),
          repo: trigger.repo.trim(),
          events: parseEvents(trigger.eventsText),
          secret: trigger.secret.trim(),
        })),
    };

    const result = await createRoutine(input);
    if (!result) return;
    onCreated?.(result);
    if (result.generatedSecrets.length > 0) {
      setCreatedRoutineName(result.routine.name);
      setGeneratedSecrets(result.generatedSecrets);
      return;
    }
    setOpen(false);
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent
        className={getDialogWidthClassName(
          "thin",
          "flex max-h-[calc(100vh-3rem)] flex-col gap-0 overflow-hidden p-6 sm:max-h-[calc(100vh-5rem)]",
        )}
      >
        <div className="min-h-0 flex-1 space-y-6 overflow-y-auto pr-1">
          <DialogHeader>
            <DialogTitle className="text-xl">Create routine</DialogTitle>
            <DialogDescription>
              Configure how an agent routine is triggered and what prompt it runs.
            </DialogDescription>
          </DialogHeader>

          {generatedSecrets.length > 0 ? (
            <div className="space-y-4">
              <div className="rounded-lg border border-border bg-muted/30 p-4">
                <div className="flex items-center gap-2 text-sm font-medium">
                  <KeyRoundIcon className="size-4 text-muted-foreground" />
                  API secrets for {createdRoutineName}
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  These secrets are only shown once.
                </p>
              </div>
              <div className="space-y-3">
                {generatedSecrets.map((secret) => (
                  <div
                    key={secret.triggerId}
                    className="rounded-lg border border-border p-3"
                  >
                    <div className="text-sm font-medium">{secret.name}</div>
                    <code className="mt-2 block break-all rounded-md bg-muted px-2 py-1.5 font-mono text-xs">
                      {secret.secret}
                    </code>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <>
              <div className="grid gap-4 md:grid-cols-[minmax(0,1fr)_220px]">
                <div className="space-y-2">
                  <Label htmlFor="routine-name">Name</Label>
                  <Input
                    id="routine-name"
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                    placeholder="Daily incident review"
                  />
                </div>
                <div className="space-y-2">
                  <Label>Agent</Label>
                  {fixedAgentId ? (
                    <div className="flex h-8 items-center rounded-lg border border-input px-2.5 text-sm text-muted-foreground">
                      {fixedAgentName ?? fixedAgentId}
                    </div>
                  ) : (
                    <Select
                      value={agentId}
                      onValueChange={(value) => value && setAgentId(value)}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select an agent">
                          {selectedAgent?.name}
                        </SelectValue>
                      </SelectTrigger>
                      <SelectContent>
                        {(agents ?? []).map((agent) => (
                          <SelectItem key={agent.id} value={agent.id}>
                            {agent.name}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="routine-prompt">
                  Prompt
                  <HelpTooltip>
                    This prompt is sent to the agent whenever any trigger fires.
                  </HelpTooltip>
                </Label>
                <Textarea
                  id="routine-prompt"
                  value={prompt}
                  onChange={(event) => setPrompt(event.target.value)}
                  placeholder="Describe what the agent should do when this routine runs..."
                  rows={5}
                />
              </div>

              <Separator />

              <div className="space-y-3 pb-6">
                <div>
                  <Label>Triggers</Label>
                  <p className="text-xs text-muted-foreground">
                    Add one or more ways to start this routine.
                  </p>
                </div>
                <div className="grid gap-2 sm:grid-cols-3">
                  {TRIGGER_OPTIONS.map((option) => (
                    <button
                      key={option.type}
                      type="button"
                      onClick={() => addTrigger(option.type)}
                      className="rounded-lg border border-border px-3 py-3 text-left transition-colors hover:bg-muted/50"
                    >
                      <span className="flex items-center gap-2 text-sm font-medium">
                        {option.icon}
                        {option.label}
                      </span>
                      <span className="mt-1 block text-xs text-muted-foreground">
                        {option.description}
                      </span>
                    </button>
                  ))}
                </div>

                <div className="space-y-4">
                  {triggers.map((trigger, index) => (
                    <TriggerSection
                      key={trigger.id}
                      index={index}
                      trigger={trigger}
                      onUpdate={(patch) => updateTrigger(trigger.id, patch)}
                      onRemove={() => removeTrigger(trigger.id)}
                    />
                  ))}
                </div>
                <div ref={triggerEndRef} />
              </div>
            </>
          )}
        </div>

        <div className="border-t border-border">
          <div className="flex items-center justify-end gap-2 pt-4">
            {generatedSecrets.length > 0 ? (
              <Button type="button" onClick={() => setOpen(false)}>
                Done
              </Button>
            ) : (
              <>
                <Button
                  type="button"
                  variant="ghost"
                  disabled={creating}
                  onClick={() => setOpen(false)}
                >
                  Cancel
                </Button>
                <WithTooltip tooltip="Create this routine with the selected triggers.">
                  <Button
                    type="button"
                    disabled={!canCreate || creating}
                    onClick={submit}
                  >
                    <PlusIcon className="size-4" />
                    {creating ? "Creating..." : "Create routine"}
                  </Button>
                </WithTooltip>
              </>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}

function TriggerSection({
  index,
  trigger,
  onUpdate,
  onRemove,
}: {
  index: number;
  trigger: TriggerDraft;
  onUpdate: (patch: Partial<TriggerDraft>) => void;
  onRemove: () => void;
}) {
  const title =
    trigger.type === "api"
      ? "API trigger"
      : trigger.type === "github"
        ? "GitHub trigger"
        : "Schedule trigger";

  return (
    <div className="rounded-lg border border-border p-4">
      <div className="mb-4 flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          {trigger.type === "api" && <Code2Icon className="size-4" />}
          {trigger.type === "github" && <GitBranchIcon className="size-4" />}
          {trigger.type === "schedule" && (
            <CalendarClockIcon className="size-4" />
          )}
          {title} {index + 1}
        </div>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="text-muted-foreground hover:text-destructive"
          onClick={onRemove}
        >
          <Trash2Icon className="size-4" />
        </Button>
      </div>

      <div className="space-y-4">
        <div className="space-y-2">
          <Label>Name</Label>
          <Input
            value={trigger.name}
            onChange={(event) => onUpdate({ name: event.target.value })}
            placeholder={title}
          />
        </div>

        {trigger.type === "api" && (
          <p className="rounded-md bg-muted/50 px-3 py-2 text-xs text-muted-foreground">
            Creating this routine will generate a one-time API secret.
          </p>
        )}

        {trigger.type === "github" && (
          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label>Owner</Label>
              <Input
                value={trigger.owner}
                onChange={(event) => onUpdate({ owner: event.target.value })}
                placeholder="acme"
              />
            </div>
            <div className="space-y-2">
              <Label>Repository</Label>
              <Input
                value={trigger.repo}
                onChange={(event) => onUpdate({ repo: event.target.value })}
                placeholder="backend"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label>Events</Label>
              <Input
                value={trigger.eventsText}
                onChange={(event) => onUpdate({ eventsText: event.target.value })}
                placeholder="push, pull_request"
              />
            </div>
            <div className="space-y-2 md:col-span-2">
              <Label>Webhook secret</Label>
              <Input
                value={trigger.secret}
                onChange={(event) => onUpdate({ secret: event.target.value })}
                placeholder="Shared webhook secret"
              />
            </div>
          </div>
        )}

        {trigger.type === "schedule" && (
          <ScheduleTriggerFields trigger={trigger} onUpdate={onUpdate} />
        )}
      </div>
    </div>
  );
}

function ScheduleTriggerFields({
  trigger,
  onUpdate,
}: {
  trigger: ScheduleDraft;
  onUpdate: (patch: Partial<TriggerDraft>) => void;
}) {
  const needsTime = !["every-30-min", "every-hour"].includes(trigger.frequency);
  const needsDayOfWeek = trigger.frequency === "every-week";
  const needsDayOfMonth = trigger.frequency === "every-month";

  return (
    <div className="space-y-4">
      <Label className="gap-2 text-xs text-muted-foreground">
        <Switch
          size="sm"
          checked={trigger.advanced}
          onCheckedChange={(advanced) => onUpdate({ advanced })}
        />
        Advanced cron expression
      </Label>

      {trigger.advanced ? (
        <div className="space-y-2">
          <Label>Expression</Label>
          <Input
            value={trigger.expression}
            onChange={(event) => onUpdate({ expression: event.target.value })}
            placeholder="0 9 * * *"
          />
        </div>
      ) : (
        <>
          <div className="space-y-2">
            <Label>Frequency</Label>
            <Select
              value={trigger.frequency}
              onValueChange={(value) =>
                value && onUpdate({ frequency: value as Frequency })
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {FREQUENCIES.map((frequency) => (
                  <SelectItem key={frequency.value} value={frequency.value}>
                    {frequency.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {needsTime && (
            <div className="flex items-center gap-2">
              <Label className="shrink-0 text-xs text-muted-foreground">At</Label>
              <Select
                value={trigger.hour}
                onValueChange={(value) => value && onUpdate({ hour: value })}
              >
                <SelectTrigger className="w-[80px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Array.from({ length: 24 }, (_, hour) => (
                    <SelectItem
                      key={hour}
                      value={hour.toString().padStart(2, "0")}
                    >
                      {hour.toString().padStart(2, "0")}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <span className="text-muted-foreground">:</span>
              <Select
                value={trigger.minute}
                onValueChange={(value) => value && onUpdate({ minute: value })}
              >
                <SelectTrigger className="w-[80px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {["00", "15", "30", "45"].map((minute) => (
                    <SelectItem key={minute} value={minute}>
                      {minute}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <span className="text-xs text-muted-foreground">UTC</span>
            </div>
          )}

          {needsDayOfWeek && (
            <div className="flex items-center gap-2">
              <Label className="shrink-0 text-xs text-muted-foreground">On</Label>
              <Select
                value={trigger.dayOfWeek}
                onValueChange={(value) => value && onUpdate({ dayOfWeek: value })}
              >
                <SelectTrigger className="w-[140px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {DAYS_OF_WEEK.map((day) => (
                    <SelectItem key={day.value} value={day.value}>
                      {day.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {needsDayOfMonth && (
            <div className="flex items-center gap-2">
              <Label className="shrink-0 text-xs text-muted-foreground">
                On day
              </Label>
              <Select
                value={trigger.dayOfMonth}
                onValueChange={(value) =>
                  value && onUpdate({ dayOfMonth: value })
                }
              >
                <SelectTrigger className="w-[80px]">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Array.from({ length: 28 }, (_, index) => (
                    <SelectItem key={index + 1} value={(index + 1).toString()}>
                      {index + 1}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}
        </>
      )}

      <p className="rounded-md bg-muted/50 px-3 py-2 font-mono text-xs text-muted-foreground">
        {buildCronExpression(trigger)}
      </p>
    </div>
  );
}

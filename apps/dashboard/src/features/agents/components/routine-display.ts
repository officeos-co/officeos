import type { AgentRoutine, RoutineTrigger } from "../api/useRoutines";
import {
  getScheduleTriggers,
  getTriggerKinds,
  parseGitHubTriggerConfig,
  parseScheduleExpression,
} from "../api/useRoutines";

export function describeCronExpression(expression: string): string {
  const known: Record<string, string> = {
    "*/30 * * * *": "Every 30 minutes",
    "0 * * * *": "Every hour",
    "0 9 * * *": "Every day at 09:00 UTC",
  };
  if (known[expression]) return known[expression];

  const parts = expression.split(" ");
  if (parts.length !== 5) return expression;
  const [min, hour, dom, , dow] = parts;
  const time = `${hour.padStart(2, "0")}:${min.padStart(2, "0")} UTC`;

  if (dom !== "*" && dow === "*") return `Monthly on day ${dom} at ${time}`;
  if (dow === "1-5") return `Weekdays at ${time}`;
  if (dow !== "*") return `Weekly on day ${dow} at ${time}`;
  if (hour !== "*" && dom === "*" && dow === "*") return `Daily at ${time}`;

  return expression;
}

export function isHeartbeatCron(expression: string): boolean {
  return expression === "*/30 * * * *";
}

export function triggerKindLabel(kind: string): string {
  if (kind === "api") return "API";
  if (kind === "github") return "GitHub";
  if (kind === "schedule") return "Schedule";
  return kind;
}

export function describeTrigger(trigger: RoutineTrigger): string {
  if (trigger.kind === "schedule") {
    const expression = parseScheduleExpression(trigger);
    return expression ? describeCronExpression(expression) : "Schedule";
  }
  if (trigger.kind === "github") {
    const config = parseGitHubTriggerConfig(trigger);
    const repo = [config.owner, config.repo].filter(Boolean).join("/");
    const events = config.events?.length ? config.events.join(", ") : "events";
    return repo ? `${repo} (${events})` : "GitHub webhook";
  }
  if (trigger.kind === "api") return "Manual API trigger";
  return trigger.name;
}

export function routineTriggerSummary(routine: AgentRoutine): string {
  const kinds = getTriggerKinds(routine).map(triggerKindLabel);
  if (kinds.length === 0) return "No triggers";
  return kinds.join(", ");
}

export function routineScheduleSummary(routine: AgentRoutine): string {
  const schedules = getScheduleTriggers(routine);
  if (schedules.length === 0) return "No schedule";
  return schedules.map(describeTrigger).join(", ");
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "Never";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Unknown" : date.toLocaleString();
}

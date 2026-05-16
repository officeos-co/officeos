export const ResourceKinds = [
  "agents",
  "runs",
  "channels",
  "credentials",
  "routines",
  "browsers",
  "memorystores",
  "engines",
  "providers",
  "models",
] as const;

export type ResourceKind = (typeof ResourceKinds)[number];

export type LoadState = "idle" | "loading" | "ready" | "error";

export interface ResourceCategory {
  label: string;
  kind: ResourceKind;
  endpoint: string;
  descriptionKey?: string;
}

export const ResourceCategories: ResourceCategory[] = [
  { label: "Agents", kind: "agents", endpoint: "/api/v1/resources/agents", descriptionKey: "model" },
  { label: "Runs", kind: "runs", endpoint: "/api/v1/resources/runs", descriptionKey: "phase" },
  { label: "Channels", kind: "channels", endpoint: "/api/v1/resources/channels", descriptionKey: "type" },
  { label: "Credentials", kind: "credentials", endpoint: "/api/v1/resources/credentials", descriptionKey: "provider" },
  { label: "Routines", kind: "routines", endpoint: "/api/v1/resources/routines", descriptionKey: "agentName" },
  { label: "Browsers", kind: "browsers", endpoint: "/api/v1/resources/browsers", descriptionKey: "displayName" },
  { label: "Memory Stores", kind: "memorystores", endpoint: "/api/v1/resources/memorystores", descriptionKey: "displayName" },
  { label: "Engines", kind: "engines", endpoint: "/api/v1/resources/engines", descriptionKey: "type" },
  { label: "Providers", kind: "providers", endpoint: "/api/v1/providers", descriptionKey: "displayName" },
  { label: "Models", kind: "models", endpoint: "/api/v1/models", descriptionKey: "provider" },
];

export type ResourceValue = Record<string, unknown>;

export interface DashboardUser {
  id: string;
  email: string;
  name?: string | null;
  displayName?: string | null;
}

export interface ResourceHealth {
  status?: string;
  state?: "green" | "orange" | "red" | "idle" | string;
  reason?: string;
  message?: string;
  lastBootstrapRunId?: string | null;
  lastBootstrapAt?: string | null;
  lastSuccessfulBootstrapAt?: string | null;
}

export function resourceName(value: unknown): string {
  if (!value || typeof value !== "object") return String(value ?? "");
  const record = value as Record<string, unknown>;
  const direct = firstString(record.name, record.displayName, record.id, record.Id);
  if (direct) return direct;
  const metadata = record.metadata;
  if (metadata && typeof metadata === "object") {
    const metadataName = (metadata as Record<string, unknown>).name;
    if (typeof metadataName === "string") return metadataName;
  }
  return "";
}

export function resourceId(value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const record = value as Record<string, unknown>;
  return firstString(record.id, record.Id, record.name) ?? "";
}

export function resourceStatus(value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const record = value as Record<string, unknown>;
  const health = resourceHealth(value);
  if (health?.status) return health.status;
  if (typeof record.status === "string") return record.status;
  if (typeof record.phase === "string") return record.phase;
  if (typeof record.enabled === "boolean") return record.enabled ? "enabled" : "disabled";
  if (typeof record.configured === "boolean") return record.configured ? "configured" : "unconfigured";
  return "";
}

export function isActiveResource(value: unknown): boolean {
  const health = resourceHealth(value);
  if (health?.state) return health.state === "green";
  const status = resourceStatus(value).toLowerCase();
  if (["active", "running", "enabled", "configured", "ready", "succeeded"].includes(status)) return true;
  if (!value || typeof value !== "object") return false;
  const record = value as Record<string, unknown>;
  return record.enabled === true || record.configured === true;
}

export function resourceHealth(value: unknown): ResourceHealth | null {
  if (!value || typeof value !== "object") return null;
  const health = (value as Record<string, unknown>).health;
  return health && typeof health === "object" ? health as ResourceHealth : null;
}

export function resourceHealthState(value: unknown): "green" | "orange" | "red" | "idle" | "neutral" {
  const health = resourceHealth(value);
  if (health?.state === "green" || health?.state === "orange" || health?.state === "red" || health?.state === "idle") return health.state;
  const status = resourceStatus(value).toLowerCase();
  if (["active", "running", "enabled", "configured", "ready", "succeeded", "healthy"].includes(status)) return "green";
  if (["idle"].includes(status)) return "idle";
  if (["pending", "queued", "booting", "restarting", "working", "degraded"].includes(status)) return "orange";
  if (["error", "failed", "disabled", "unconfigured", "canceled", "cancelled"].includes(status)) return "red";
  return "neutral";
}

export function resourceHealthReason(value: unknown): string {
  const health = resourceHealth(value);
  return health?.reason || resourceStatus(value) || "-";
}

export function resourceTimestamp(value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const record = value as Record<string, unknown>;
  const raw = firstString(record.updatedAt, record.createdAt, record.completedAt, record.lastActivityAt);
  if (!raw) return "";
  const date = new Date(raw);
  return Number.isNaN(date.getTime()) ? raw : date.toLocaleString();
}

export function describeResource(category: ResourceCategory, resource: ResourceValue): string {
  if (!category.descriptionKey) return "";
  const value = resource[category.descriptionKey];
  return typeof value === "string" ? value : "";
}

function firstString(...values: unknown[]): string | undefined {
  for (const value of values) {
    if (typeof value === "string" && value.trim()) return value;
    if (typeof value === "number") return String(value);
  }
  return undefined;
}

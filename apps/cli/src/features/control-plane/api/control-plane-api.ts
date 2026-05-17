import { ApiClient } from "../../../lib/api-client";

const ApiBasePath = "/api/v1";

export interface AgentWorkResponse {
  kind: "AgentWork";
  agentId: string;
  sessionId?: string | null;
  agentName: string;
  workLogId: string;
  correlationId?: string | null;
  status: string;
  createdAt: string;
}

export interface ResourceLogOptions {
  tail?: number;
  since?: string;
  sinceTime?: string;
  type?: string;
  severity?: string;
  follow?: boolean;
}

export interface ResourceDescriptor {
  kind: string;
  singular: string;
  aliases: string[];
  displayName: string;
  description: string;
  icon: string;
  capabilities: string[];
  displayFields: string[];
}

export interface CodexProviderAuthRequest {
  accessToken: string;
  refreshToken: string;
  expiresAt?: string;
  accountEmail?: string;
  accountId?: string;
  clientId?: string;
  tokenUrl?: string;
  scopes?: string[];
}

export async function listResourceCatalog(apiUrl: string, token: string): Promise<ResourceDescriptor[]> {
  const value = await new ApiClient({ apiUrl, token }).get<unknown[]>(`${ApiBasePath}/resources`);
  return Array.isArray(value) ? value.map(coerceResourceDescriptor) : [];
}

export async function listResources(apiUrl: string, token: string, kind: string): Promise<unknown[]> {
  return await new ApiClient({ apiUrl, token }).get<unknown[]>(`${ApiBasePath}/resources/${encodeURIComponent(kind)}`);
}

export async function describeResource(apiUrl: string, token: string, kind: string, name: string): Promise<unknown> {
  return await new ApiClient({ apiUrl, token }).get<unknown>(`${ApiBasePath}/resources/${encodeURIComponent(kind)}/${encodeURIComponent(name)}`);
}

export async function deleteResource(apiUrl: string, token: string, kind: string, name: string): Promise<unknown> {
  return await new ApiClient({ apiUrl, token }).delete<unknown>(`${ApiBasePath}/resources/${encodeURIComponent(kind)}/${encodeURIComponent(name)}`);
}

export async function sendAgentMessage(apiUrl: string, token: string, agentRef: string, message: string): Promise<AgentWorkResponse> {
  return await new ApiClient({ apiUrl, token }).post<AgentWorkResponse>(`${ApiBasePath}/resources/agents/${encodeURIComponent(agentRef)}/messages`, {
    message,
    purpose: "manual",
  });
}

export async function getResourceLogs(apiUrl: string, token: string, kind: string, name: string, options: ResourceLogOptions = {}): Promise<string> {
  const query = new URLSearchParams();
  if (options.tail !== undefined) query.set("tail", String(options.tail));
  if (options.since) query.set("since", options.since);
  if (options.sinceTime) query.set("sinceTime", options.sinceTime);
  if (options.type) query.set("type", options.type);
  if (options.severity) query.set("severity", options.severity);
  if (options.follow) query.set("follow", "true");

  const suffix = query.size > 0 ? `?${query.toString()}` : "";
  return await new ApiClient({ apiUrl, token }).getText(
    `${ApiBasePath}/resources/${encodeURIComponent(kind)}/${encodeURIComponent(name)}/logs${suffix}`,
  );
}

export async function listProviders(apiUrl: string, token: string): Promise<unknown[]> {
  return await listResources(apiUrl, token, "providers");
}

export async function listModels(apiUrl: string, token: string): Promise<unknown[]> {
  return await listResources(apiUrl, token, "models");
}

export async function authenticateCodexProvider(apiUrl: string, token: string, body: CodexProviderAuthRequest): Promise<unknown> {
  return await new ApiClient({ apiUrl, token }).post<unknown>(`${ApiBasePath}/resources/providers/codex/auth`, body);
}

function coerceResourceDescriptor(value: unknown): ResourceDescriptor {
  const record = value && typeof value === "object"
    ? value as Record<string, unknown>
    : {};

  return {
    kind: stringValue(record.kind),
    singular: stringValue(record.singular),
    aliases: arrayOfStrings(record.aliases),
    displayName: stringValue(record.displayName || record.kind),
    description: stringValue(record.description),
    icon: stringValue(record.icon || "folder"),
    capabilities: arrayOfCapabilityNames(record.capabilities),
    displayFields: arrayOfStrings(record.displayFields),
  };
}

function arrayOfCapabilityNames(value: unknown): string[] {
  return Array.isArray(value)
    ? value.map(capabilityName).filter((item): item is string => item.length > 0)
    : [];
}

function capabilityName(value: unknown): string {
  if (typeof value === "string") return value;
  if (value && typeof value === "object") {
    const name = (value as Record<string, unknown>).name;
    return typeof name === "string" ? name : "";
  }
  return "";
}

function arrayOfStrings(value: unknown): string[] {
  return Array.isArray(value)
    ? value.filter((item): item is string => typeof item === "string")
    : [];
}

function stringValue(value: unknown): string {
  return typeof value === "string" ? value : "";
}

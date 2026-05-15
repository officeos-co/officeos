import { ApiClient } from "../../../lib/api-client";

const ApiBasePath = "/api/v1";

export interface AgentWorkResponse {
  kind: "AgentWork";
  agentId: string;
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
  return await new ApiClient({ apiUrl, token }).get<unknown[]>(`${ApiBasePath}/providers`);
}

export async function listModels(apiUrl: string, token: string): Promise<unknown[]> {
  return await new ApiClient({ apiUrl, token }).get<unknown[]>(`${ApiBasePath}/models`);
}

import { ApiClient } from "../../../lib/api-client";

const ApiBasePath = "/api/v1";

export interface RunResponse {
  run?: RunRecord;
  engineType?: string;
  engineRef?: string;
}

export interface RunRecord {
  id: string;
  agentId: string;
  kind: string;
  status: string;
  name: string;
  prompt: string;
  result?: string | null;
  error?: string | null;
  createdAt: string;
  updatedAt: string;
  completedAt?: string | null;
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

export async function createRun(apiUrl: string, token: string, agentRef: string, task: string, engineRef: string, wait: boolean): Promise<RunResponse> {
  return await new ApiClient({ apiUrl, token }).post<RunResponse>(`${ApiBasePath}/runs`, {
    agentRef,
    task,
    engineRef,
    wait,
  });
}

export async function listRuns(apiUrl: string, token: string): Promise<RunRecord[]> {
  return await new ApiClient({ apiUrl, token }).get<RunRecord[]>(`${ApiBasePath}/runs`);
}

export async function getRun(apiUrl: string, token: string, id: string): Promise<RunRecord> {
  return await new ApiClient({ apiUrl, token }).get<RunRecord>(`${ApiBasePath}/runs/${encodeURIComponent(id)}`);
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

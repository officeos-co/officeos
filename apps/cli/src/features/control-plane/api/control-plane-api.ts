import { ApiClient } from "../../../lib/api-client";

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

export interface RunLogResponse {
  entries: Array<{
    id: string;
    time: string;
    type: string;
    content: string;
  }>;
}

export async function listResources(apiUrl: string, token: string, kind: string): Promise<unknown[]> {
  return await new ApiClient({ apiUrl, token }).get<unknown[]>(`/api/control-plane/v1/resources/${encodeURIComponent(kind)}`);
}

export async function describeResource(apiUrl: string, token: string, kind: string, name: string): Promise<unknown> {
  return await new ApiClient({ apiUrl, token }).get<unknown>(`/api/control-plane/v1/resources/${encodeURIComponent(kind)}/${encodeURIComponent(name)}`);
}

export async function deleteResource(apiUrl: string, token: string, kind: string, name: string): Promise<unknown> {
  return await new ApiClient({ apiUrl, token }).delete<unknown>(`/api/control-plane/v1/resources/${encodeURIComponent(kind)}/${encodeURIComponent(name)}`);
}

export async function createRun(apiUrl: string, token: string, agentRef: string, task: string, engineRef: string, wait: boolean): Promise<RunResponse> {
  return await new ApiClient({ apiUrl, token }).post<RunResponse>("/api/control-plane/v1/runs", {
    agentRef,
    task,
    engineRef,
    wait,
  });
}

export async function listRuns(apiUrl: string, token: string): Promise<RunRecord[]> {
  return await new ApiClient({ apiUrl, token }).get<RunRecord[]>("/api/control-plane/v1/runs");
}

export async function getRun(apiUrl: string, token: string, id: string): Promise<RunRecord> {
  return await new ApiClient({ apiUrl, token }).get<RunRecord>(`/api/control-plane/v1/runs/${encodeURIComponent(id)}`);
}

export async function getRunLogs(apiUrl: string, token: string, id: string): Promise<RunLogResponse> {
  return await new ApiClient({ apiUrl, token }).get<RunLogResponse>(`/api/control-plane/v1/runs/${encodeURIComponent(id)}/logs`);
}

export async function listProviders(apiUrl: string, token: string): Promise<unknown[]> {
  return await new ApiClient({ apiUrl, token }).get<unknown[]>("/api/control-plane/v1/providers");
}

export async function listModels(apiUrl: string, token: string): Promise<unknown[]> {
  return await new ApiClient({ apiUrl, token }).get<unknown[]>("/api/control-plane/v1/models");
}

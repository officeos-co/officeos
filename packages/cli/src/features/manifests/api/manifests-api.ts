import { ApiClient } from "../../../lib/api-client";

export interface DeclarativeChange {
  kind: string;
  name: string;
  action: string;
  agentId?: string | null;
  message?: string | null;
}

export interface ValidateResponse {
  valid: boolean;
  errors: string[];
  resources: string[];
}

export interface DiffResponse {
  changes: DeclarativeChange[];
}

export async function validateManifest(
  apiUrl: string,
  token: string,
  manifest: string,
): Promise<ValidateResponse> {
  return await new ApiClient({ apiUrl, token }).post<ValidateResponse>(
    "/api/declarative/validate",
    { manifest },
  );
}

export async function diffManifest(
  apiUrl: string,
  token: string,
  manifest: string,
): Promise<DiffResponse> {
  return await new ApiClient({ apiUrl, token }).post<DiffResponse>(
    "/api/declarative/diff",
    { manifest },
  );
}

export async function applyManifest(
  apiUrl: string,
  token: string,
  manifest: string,
): Promise<DiffResponse> {
  return await new ApiClient({ apiUrl, token }).post<DiffResponse>(
    "/api/declarative/apply",
    { manifest },
  );
}

export async function exportAgent(
  apiUrl: string,
  token: string,
  name: string,
): Promise<string> {
  return await new ApiClient({ apiUrl, token }).getText(
    `/api/declarative/agents/${encodeURIComponent(name)}/export`,
  );
}

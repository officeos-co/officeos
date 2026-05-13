import { ApiClient } from "../../../lib/api-client";

export interface DeclarativeChange {
  kind: string;
  name: string;
  action: string;
  resourceId?: string | null;
  message?: string | null;
}

export interface DeclarativeValidationError {
  kind: string;
  name: string;
  message: string;
}

export interface ValidateResponse {
  valid: boolean;
  errors: DeclarativeValidationError[];
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

export async function exportWorkspace(
  apiUrl: string,
  token: string,
): Promise<string> {
  return await new ApiClient({ apiUrl, token }).getText("/api/declarative/export");
}

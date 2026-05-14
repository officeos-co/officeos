import { ApiClient } from "../../../lib/api-client";

const ApiBasePath = "/api/v1";

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
    `${ApiBasePath}/manifests/validate`,
    { manifest },
  );
}

export async function diffManifest(
  apiUrl: string,
  token: string,
  manifest: string,
): Promise<DiffResponse> {
  return await new ApiClient({ apiUrl, token }).post<DiffResponse>(
    `${ApiBasePath}/manifests/diff`,
    { manifest },
  );
}

export async function applyManifest(
  apiUrl: string,
  token: string,
  manifest: string,
): Promise<DiffResponse> {
  return await new ApiClient({ apiUrl, token }).post<DiffResponse>(
    `${ApiBasePath}/manifests/apply`,
    { manifest },
  );
}

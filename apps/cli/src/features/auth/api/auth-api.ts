import { ApiClient } from "../../../lib/api-client";

const ApiBasePath = "/api/v1";

export interface DeviceCodeResponse {
  deviceCode: string;
  userCode: string;
  verificationUri: string;
  verificationUriComplete: string;
  expiresAt: string;
  intervalSeconds: number;
}

export interface DeviceTokenResponse {
  status: "pending" | "authorized" | "expired";
  accessToken: string | null;
  expiresAt: string | null;
  intervalSeconds: number;
}

export interface MeResponse {
  id: string;
  email: string;
  name?: string | null;
  displayName?: string | null;
}

export async function createDeviceCode(apiUrl: string, runnerName?: string): Promise<DeviceCodeResponse> {
  return await new ApiClient({ apiUrl }).post<DeviceCodeResponse>(`${ApiBasePath}/auth/device/code`, { runnerName });
}

export async function pollDeviceToken(apiUrl: string, deviceCode: string): Promise<DeviceTokenResponse> {
  return await new ApiClient({ apiUrl }).post<DeviceTokenResponse>(`${ApiBasePath}/auth/device/token`, { deviceCode });
}

export async function getMe(apiUrl: string, token: string): Promise<MeResponse> {
  return await new ApiClient({ apiUrl, token }).get<MeResponse>(`${ApiBasePath}/me`);
}

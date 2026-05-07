import { getEnvConfig } from "@/lib/env";

export function buildOAuthUrl(provider: string, returnTo?: string): string {
  const url = new URL(`/api/auth/${provider}`, getEnvConfig().apiUrl);
  if (returnTo) url.searchParams.set("returnTo", returnTo);
  return url.toString();
}

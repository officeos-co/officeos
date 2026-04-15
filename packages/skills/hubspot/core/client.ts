export const HUBSPOT_API = "https://api.hubapi.com";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function hsHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function hsGet(ctx: Ctx, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${HUBSPOT_API}${path}${qs}`, {
    headers: hsHeaders(ctx.credentials.access_token),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`HubSpot API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function hsPost(ctx: Ctx, path: string, body?: unknown, method = "POST") {
  const res = await ctx.fetch(`${HUBSPOT_API}${path}`, {
    method,
    headers: hsHeaders(ctx.credentials.access_token),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HubSpot API ${res.status}: ${text}`);
  }
  // DELETE may return 204 with no body
  if (res.status === 204) return { success: true };
  return res.json();
}

export async function hsDelete(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${HUBSPOT_API}${path}`, {
    method: "DELETE",
    headers: hsHeaders(ctx.credentials.access_token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`HubSpot API ${res.status}: ${text}`);
  }
  return { success: true };
}

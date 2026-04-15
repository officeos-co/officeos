// DigitalOcean API client helpers — no skill-sdk imports

export const DO_API = "https://api.digitalocean.com/v2";

export type DoCtx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function doHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

export async function doFetch(ctx: DoCtx, path: string, init?: RequestInit): Promise<any> {
  const res = await ctx.fetch(`${DO_API}${path}`, {
    ...init,
    headers: { ...doHeaders(ctx.credentials.token), ...init?.headers },
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`DigitalOcean API ${res.status}: ${text}`);
  }
  if (res.status === 204) return null;
  return res.json();
}

export async function doGet(ctx: DoCtx, path: string): Promise<any> {
  return doFetch(ctx, path);
}

export async function doPost(ctx: DoCtx, path: string, body: unknown): Promise<any> {
  return doFetch(ctx, path, { method: "POST", body: JSON.stringify(body) });
}

export async function doPut(ctx: DoCtx, path: string, body: unknown): Promise<any> {
  return doFetch(ctx, path, { method: "PUT", body: JSON.stringify(body) });
}

export async function doDelete(ctx: DoCtx, path: string): Promise<any> {
  return doFetch(ctx, path, { method: "DELETE" });
}

export function qs(params: Record<string, string | number | undefined>): string {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`);
  return parts.length ? "?" + parts.join("&") : "";
}

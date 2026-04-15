export const META_API = "https://graph.facebook.com/v21.0";

export type MetaCtx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function metaHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function metaGet(ctx: MetaCtx, path: string, params?: Record<string, string>): Promise<any> {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${META_API}${path}${qs}`, {
    headers: metaHeaders(ctx.credentials.access_token),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Meta API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function metaPost(ctx: MetaCtx, path: string, body?: Record<string, unknown>, method = "POST"): Promise<any> {
  const res = await ctx.fetch(`${META_API}${path}`, {
    method,
    headers: {
      ...metaHeaders(ctx.credentials.access_token),
      "Content-Type": "application/json",
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Meta API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function metaDelete(ctx: MetaCtx, path: string): Promise<any> {
  const res = await ctx.fetch(`${META_API}${path}`, {
    method: "DELETE",
    headers: metaHeaders(ctx.credentials.access_token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Meta API ${res.status}: ${text}`);
  }
  return res.json();
}

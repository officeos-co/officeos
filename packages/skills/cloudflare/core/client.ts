export const CF_API = "https://api.cloudflare.com/client/v4";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function cfHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

export async function cfFetch(ctx: Ctx, path: string, init?: RequestInit) {
  const res = await ctx.fetch(`${CF_API}${path}`, {
    ...init,
    headers: { ...cfHeaders(ctx.credentials.api_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Cloudflare API ${res.status}: ${body}`);
  }
  const json = await res.json();
  if (!json.success && json.errors?.length) {
    throw new Error(`Cloudflare API error: ${json.errors.map((e: any) => e.message).join(", ")}`);
  }
  return json.result;
}

export async function cfPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${CF_API}${path}`, {
    method,
    headers: cfHeaders(ctx.credentials.api_token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Cloudflare API ${res.status}: ${text}`);
  }
  const json = await res.json();
  if (!json.success && json.errors?.length) {
    throw new Error(`Cloudflare API error: ${json.errors.map((e: any) => e.message).join(", ")}`);
  }
  return json.result;
}

export async function cfDelete(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${CF_API}${path}`, {
    method: "DELETE",
    headers: cfHeaders(ctx.credentials.api_token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Cloudflare API ${res.status}: ${text}`);
  }
  const json = await res.json();
  return json.result;
}

export async function getAccountId(ctx: Ctx): Promise<string> {
  const accounts = await cfFetch(ctx, `/accounts?per_page=1`);
  const accountId = (Array.isArray(accounts) ? accounts : [])[0]?.id;
  if (!accountId) throw new Error("No Cloudflare account found for this token.");
  return accountId;
}

export function enc(s: string) {
  return encodeURIComponent(s);
}
